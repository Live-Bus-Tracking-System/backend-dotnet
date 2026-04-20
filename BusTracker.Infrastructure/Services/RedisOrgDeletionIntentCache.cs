using BusTracker.Application.Common.Interfaces;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace BusTracker.Infrastructure.Services
{
    public class RedisOrgDeletionIntentCache : IOrgDeletionIntentCache
    {
        private readonly IDatabase _redis;
        private const string OtpPrefix = "org:delete:otp:";
        private const string ConfirmPrefix = "org:delete:confirmed:";

        public RedisOrgDeletionIntentCache(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        public async Task StoreOtpIntentAsync(string intentId, OrgDeletionOtpIntent intent, TimeSpan ttl)
        {
            var key = OtpPrefix + intentId;
            var json = JsonSerializer.Serialize(intent);
            await _redis.StringSetAsync(key, json, ttl);
        }

        public async Task<OrgDeletionOtpIntent?> GetOtpIntentAsync(string intentId)
        {
            var key = OtpPrefix + intentId;
            var json = await _redis.StringGetAsync(key);
            if (json.IsNullOrEmpty) return null;

            return JsonSerializer.Deserialize<OrgDeletionOtpIntent>(json.ToString());
        }

        public async Task RemoveOtpIntentAsync(string intentId)
        {
            var key = OtpPrefix + intentId;
            await _redis.KeyDeleteAsync(key);
        }

        public async Task StoreConfirmTokenAsync(string confirmToken, OrgDeletionConfirmIntent intent, TimeSpan ttl)
        {
            var key = ConfirmPrefix + confirmToken;
            var json = JsonSerializer.Serialize(intent);
            await _redis.StringSetAsync(key, json, ttl);
        }

        public async Task<OrgDeletionConfirmIntent?> GetConfirmIntentAsync(string confirmToken)
        {
            var key = ConfirmPrefix + confirmToken;
            var json = await _redis.StringGetAsync(key);
            if (json.IsNullOrEmpty) return null;

            return JsonSerializer.Deserialize<OrgDeletionConfirmIntent>(json.ToString());
        }

        public async Task RemoveConfirmIntentAsync(string confirmToken)
        {
            var key = ConfirmPrefix + confirmToken;
            await _redis.KeyDeleteAsync(key);
        }
    }
}
