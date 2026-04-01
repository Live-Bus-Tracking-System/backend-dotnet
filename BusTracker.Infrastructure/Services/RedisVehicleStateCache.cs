using System.Text.Json;
using StackExchange.Redis;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Tracking.Models;

namespace BusTracker.Infrastructure.Services
{
    public class RedisVehicleStateCache : IVehicleStateCache
    {
        private readonly IDatabase _db;
        private readonly IConnectionMultiplexer _redis;
        private const string StateKeyPrefix = "vehicle:live:";
        private const string RouteKeyPrefix = "route:geom:";

        public RedisVehicleStateCache(IConnectionMultiplexer redis, IConnectionMultiplexer connectionMultiplexer)
        {
            _db = redis.GetDatabase();
            _redis = connectionMultiplexer;
        }

        // --- LIVE BUS STATE ---
        public async Task SetStateAsync(string trackerId, VehicleLiveState state)
        {
            var key = $"{StateKeyPrefix}{trackerId}";
            var json = JsonSerializer.Serialize(state);
            await _db.StringSetAsync(key, json, TimeSpan.FromHours(6));
        }

        public async Task<VehicleLiveState?> GetStateAsync(string trackerId)
        {
            var key = $"{StateKeyPrefix}{trackerId}";
            var json = await _db.StringGetAsync(key);

            if (json.IsNullOrEmpty) return null;

            return JsonSerializer.Deserialize<VehicleLiveState>(json.ToString());
        }

        public async Task<bool> IsVehicleActiveAsync(string trackerId)
        {
            var key = $"{StateKeyPrefix}{trackerId}";
            return await _db.KeyExistsAsync(key);
        }

        // --- GLOBAL ROUTE GEOMETRY CACHE ---
        public async Task<CachedRouteGeometry?> GetRouteGeometryAsync(Guid routeId)
        {
            var key = $"{RouteKeyPrefix}{routeId}";
            var json = await _db.StringGetAsync(key);

            if (json.IsNullOrEmpty) return null;

            return JsonSerializer.Deserialize<CachedRouteGeometry>(json.ToString());
        }

        public async Task<IEnumerable<CachedRouteGeometry>> GetRouteGeometriesAsync(IEnumerable<Guid> routeIds)
        {
            if (routeIds == null || !routeIds.Any())
                return Array.Empty<CachedRouteGeometry>();

            var keys = routeIds.Select(id => (RedisKey)$"{RouteKeyPrefix}{id}").ToArray();
            var values = await _db.StringGetAsync(keys);

            var geometries = new List<CachedRouteGeometry>();
            foreach (var val in values)
            {
                if (val.HasValue)
                {
                    var geometry = JsonSerializer.Deserialize<CachedRouteGeometry>(val.ToString());
                    if (geometry != null) geometries.Add(geometry);
                }
            }

            return geometries;
        }

        public async Task SetRouteGeometryAsync(Guid routeId, CachedRouteGeometry geometry)
        {
            var key = $"{RouteKeyPrefix}{routeId}";
            var json = JsonSerializer.Serialize(geometry);

            await _db.StringSetAsync(key, json, TimeSpan.FromDays(7));
        }

        // --- WATCHDOG SWEEP (SCAN + MGET) ---
        public async Task<IEnumerable<(string TrackerId, VehicleLiveState State)>> GetAllActiveVehiclesAsync()
        {
            var keys = new HashSet<RedisKey>();
            var endpoints = _redis.GetEndPoints();

            // 1. SCAN: Safely iterate over endpoints to get keys without thread-locking Redis
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                if (!server.IsConnected || server.IsReplica) continue;

                await foreach (var key in server.KeysAsync(database: _db.Database, pattern: $"{StateKeyPrefix}*"))
                {
                    keys.Add(key);
                }
            }

            if (keys.Count == 0) 
                return Array.Empty<(string, VehicleLiveState)>();

            // 2. MGET: Fetch all values in a single network round-trip for massive performance gain
            var redisKeys = keys.ToArray();
            var values = await _db.StringGetAsync(redisKeys);

            var results = new List<(string TrackerId, VehicleLiveState State)>();

            for (int i = 0; i < redisKeys.Length; i++)
            {
                if (values[i].HasValue)
                {
                    var state = JsonSerializer.Deserialize<VehicleLiveState>(values[i].ToString());
                    if (state != null)
                    {
                        // Strip the prefix to get the raw trackerId back
                        var trackerId = redisKeys[i].ToString().Substring(StateKeyPrefix.Length);
                        results.Add((trackerId, state));
                    }
                }
            }

            return results;
        }
    }
}