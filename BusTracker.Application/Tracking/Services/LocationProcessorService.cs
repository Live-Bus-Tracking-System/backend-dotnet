using BusTracker.Application.Common.Helpers;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Repository;
using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Application.Tracking.Models;
using BusTracker.Domain.Enums;
using System.Collections.Concurrent;

namespace BusTracker.Application.Tracking.Services
{
    public class LocationProcessorService : ILocationProcessorService
    {
        private readonly IVehicleStateCache _cache;
        private readonly ITrackingRepository _repository;
        private readonly ILiveTrackingBroadcaster _broadcaster;

        private const double GeofenceRadiusMeters = 50.0;
        private const double OffRouteEjectionMeters = 200.0;
        private const double MinSpeedMps = 0.5;

        //Per-tracker semaphore map. Serialises concurrent pings for the same tracker so two simultaneous requests never read the same stale state and overwrite each other.
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _trackerLocks = new();

        private static SemaphoreSlim GetTrackerLock(string trackerId) => _trackerLocks.GetOrAdd(trackerId, _ => new SemaphoreSlim(1, 1));

        public LocationProcessorService(IVehicleStateCache cache, ITrackingRepository repository, ILiveTrackingBroadcaster broadcaster)
        {
            _cache = cache;
            _repository = repository;
            _broadcaster = broadcaster;
        }

        public async Task<VehicleLiveState> ProcessPingAsync(string trackerId, LocationPingDto ping)
        {
            // Acquire a per-tracker semaphore so two simultaneous pings from the same
            // tracker are serialised. The second ping waits until the first has written to cache,
            // so it reads the freshly committed state rather than stale data.
            var trackerLock = GetTrackerLock(trackerId);
            await trackerLock.WaitAsync();
            try
            {
                // 1. FETCH STATE from cache
                var state = await _cache.GetStateAsync(trackerId);

                if (state == null)
                {
                    // COLD START: Only fetch the Expected Routes from db
                    state = await _repository.InitializeColdStateAsync(trackerId);
                    if (state == null) throw new UnauthorizedAccessException("Unknown Tracker ID");

                    // E4 FIX: Snapshot the original expected routes so we never need to call
                    // InitializeColdStateAsync again just to restore CandidateRouteIds on ejection/awakening.
                    state.ExpectedRouteIds = new List<Guid>(state.CandidateRouteIds);
                }
                else if (state.ExpectedRouteIds.Count == 0)
                {
                    // MIGRATION FIX: Existing Redis state was stored before ExpectedRouteIds was added.
                    // Repopulate the snapshot so the E4 fix works correctly going forward.
                    if (state.CandidateRouteIds.Count > 0)
                    {
                        // Still ambiguous — candidates are live, copy them.
                        state.ExpectedRouteIds = new List<Guid>(state.CandidateRouteIds);
                    }
                    else
                    {
                        // Already resolved (CandidateRouteIds was cleared on resolution).
                        // Make ONE lightweight DB call to re-populate the snapshot.
                        var coldState = await _repository.InitializeColdStateAsync(trackerId);
                        if (coldState != null)
                            state.ExpectedRouteIds = new List<Guid>(coldState.CandidateRouteIds);
                    }
                }

                // 2. CHRONOLOGICAL LOCK (Time-Travel Defense)
                if (state.TimestampUtc != DateTime.MinValue && ping.TimestampUtc <= state.TimestampUtc)
                {
                    return state;
                }

                if (ping.TimestampUtc > DateTime.UtcNow.AddMinutes(5))
                {
                    return state;
                }

                // 3. THE BOUNCER (Cooldown Logic)
                // E2 FIX: Removed the redundant inner null check — .HasValue already proves it's non-null.
                if (state.CooldownEndsAtUtc.HasValue)
                {
                    if (DateTime.UtcNow < state.CooldownEndsAtUtc.Value)
                    {
                        // Bus is on break. Update map coordinates so admin can see it, but skip heavy math.
                        state.Latitude = ping.Latitude;
                        state.Longitude = ping.Longitude;
                        state.TimestampUtc = ping.TimestampUtc;
                        await _cache.SetStateAsync(trackerId, state);

                        // Broadcast Scenario 3 (map position only) so clients can see the parked bus.
                        // No route/stop data since the route is complete. routeGeometry = null is safe here.
                        await BroadcastStateAsync(state, null, new List<CachedStop>(), 0);
                        return state;
                    }
                    else
                    {
                        // The Awakening! Cooldown expired. Bus is back on duty.
                        state.CooldownEndsAtUtc = null;
                        state.IsAmbiguous = true;

                        // E4 FIX: Restore from cached snapshot — zero DB calls needed.
                        state.CandidateRouteIds = new List<Guid>(state.ExpectedRouteIds);
                    }
                }

                // 4. GPS DRIFT DEFENSE
                var prevLat = state.Latitude;
                var prevLon = state.Longitude;
                var hasPrevLocation = state.TimestampUtc != DateTime.MinValue;

                double currentSpeedMps = 6.94; // ~25 km/h default

                if (hasPrevLocation)
                {
                    var distanceMoved = GeoCalculator.GetDistanceMeters(prevLat, prevLon, ping.Latitude, ping.Longitude);
                    var timeDiffSeconds = (ping.TimestampUtc - state.TimestampUtc).TotalSeconds;

                    if (timeDiffSeconds > 0)
                    {
                        currentSpeedMps = distanceMoved / timeDiffSeconds;
                        // Cap speed between MinSpeedMps (crawling traffic) and 30 m/s (~108 km/h highway)
                        currentSpeedMps = Math.Clamp(currentSpeedMps, MinSpeedMps, 30.0);
                    }

                    // EMA Smoothing (Freeze if < 1.0 m/s to prevent Red Light Death Spiral)
                    if (currentSpeedMps >= 1.0)
                    {
                        if (state.SmoothedSpeedMps.HasValue)
                        {
                            state.SmoothedSpeedMps = (currentSpeedMps * 0.15) + (state.SmoothedSpeedMps.Value * 0.85);
                        }
                        else
                        {
                            state.SmoothedSpeedMps = currentSpeedMps;
                        }
                    }

                    if (distanceMoved > 10.0)
                    {
                        state.Heading = GeoCalculator.GetBearing(state.Latitude, state.Longitude, ping.Latitude, ping.Longitude);
                    }
                }
                else
                {
                    state.SmoothedSpeedMps = currentSpeedMps;
                }

                state.Latitude = ping.Latitude;
                state.Longitude = ping.Longitude;
                state.TimestampUtc = ping.TimestampUtc;

                // 5. RESOLVE AMBIGUITY (Overlapping Routes)
                if (state.IsAmbiguous && state.Heading.HasValue)
                {
                    state = await ResolveAmbiguousStateAsync(state);
                }

                // ── Variables shared between step 6/7 and the broadcast (P1, P2, P3 FIX) ──────────
                // Declaring outside the if-block so BroadcastStateAsync receives pre-computed values
                // and never needs to re-fetch from cache or re-run expensive geometry math.
                CachedRouteGeometry? routeGeometry = null;
                List<CachedStop> remainingStops = new();
                double busDistanceOnLine = 0;

                // 6. SPATIAL SNAP & ZOMBIE KILLER
                if (state.ResolvedRouteId.HasValue)
                {
                    routeGeometry = await _cache.GetRouteGeometryAsync(state.ResolvedRouteId.Value); // P3: fetched ONCE
                    if (routeGeometry == null)
                    {
                        routeGeometry = await _repository.BuildRouteGeometryFromSqlAsync(state.ResolvedRouteId.Value);
                        if (routeGeometry != null)
                        {
                            routeGeometry.InitializePolylineDistances();
                            await _cache.SetRouteGeometryAsync(state.ResolvedRouteId.Value, routeGeometry);
                        }
                    }

                    if (routeGeometry == null)
                    {
                        // Deleted Route Phantom. Eject and go back to ambiguity mode.
                        state.ResolvedRouteId = null;
                        state.IsAmbiguous = true;

                        // E4 FIX: Restore from cached snapshot — zero DB calls needed.
                        state.CandidateRouteIds = new List<Guid>(state.ExpectedRouteIds);
                    }
                    else
                    {
                        // Off-Route Kidnapping. Eject if bus strays > 200m
                        if (routeGeometry.PolylineShape.Any())
                        {
                            var snapResult = GeoCalculator.SnapToPolylineWithDistance(state.Latitude, state.Longitude, routeGeometry.PolylineShape);
                            if (snapResult.OffLineDistanceMeters > OffRouteEjectionMeters)
                            {
                                state.ResolvedRouteId = null;
                                state.IsAmbiguous = true;
                                state.UpcomingStopEtas.Clear();

                                // E4 FIX: Restore from cached snapshot — zero DB calls needed.
                                state.CandidateRouteIds = new List<Guid>(state.ExpectedRouteIds);

                                // Exit early and let next ping re-resolve
                                await _cache.SetStateAsync(trackerId, state);
                                return state;
                            }
                        }

                        // E5 FIX: Replaced the tight +2 sequence window with a proximity scan.
                        // E5 FIX: Proximity-based scan — check all forward stops within GeofenceRadiusMeters*4
                        // of the current GPS point (not just ±2 by sequence). Handles fast buses and long
                        // ping intervals where a stop could be skipped by the old sequence-count window.
                        // Distances are computed once per stop to avoid double-work in Where + Select.
                        double proximityRadius = GeofenceRadiusMeters * 4;

                        var candidateStops = routeGeometry.Stops
                            .Where(s => state.Direction == RouteDirection.Inbound
                                ? s.Sequence < state.LastPassedStopSequence
                                : s.Sequence > state.LastPassedStopSequence)
                            .Select(s => (
                                Stop: s,
                                Distance: hasPrevLocation
                                    ? GeoCalculator.GetMinDistanceToLineSegment(s.Latitude, s.Longitude, prevLat, prevLon, ping.Latitude, ping.Longitude)
                                    : GeoCalculator.GetDistanceMeters(ping.Latitude, ping.Longitude, s.Latitude, s.Longitude)
                            ))
                            .Where(x => x.Distance <= proximityRadius)
                            .ToList(); // Materialise once so MinBy doesn't re-enumerate

                        // MinBy on an empty list throws — guard with Count check.
                        var closestFutureStop = candidateStops.Count > 0
                            ? candidateStops.MinBy(x => x.Distance)
                            : default;

                        bool isFinalStop = false;

                        if (closestFutureStop.Stop != null && closestFutureStop.Distance <= GeofenceRadiusMeters)
                        {
                            state.LastPassedStopSequence = closestFutureStop.Stop.Sequence;

                            // Snapshot Guid values NOW before the lambda is created.
                            // state.ResolvedRouteId is set to null below (isFinalStop path),
                            // so capturing state directly would cause InvalidOperationException at runtime.
                            var capturedRouteId = state.ResolvedRouteId!.Value;
                            var capturedVehicleId = state.VehicleId;
                            var capturedStopId = closestFutureStop.Stop.StopId;
                            var capturedTimestamp = ping.TimestampUtc;
                            var capturedSequence = state.LastPassedStopSequence;

                            // Analytics: Record the exact time the bus hit this stop
                            SafeFireAndForget(() => _repository.QueueStopArrivalRecordAsync(capturedVehicleId, capturedRouteId, capturedStopId, capturedTimestamp));

                            isFinalStop = state.Direction == RouteDirection.Inbound
                                // MinBy/MaxBy return null on empty sequences (safe) — Min/Max throw (not safe).
                                ? state.LastPassedStopSequence == (routeGeometry.Stops.MinBy(s => s.Sequence)?.Sequence ?? -1)
                                : state.LastPassedStopSequence == (routeGeometry.Stops.MaxBy(s => s.Sequence)?.Sequence ?? -1);

                            if (isFinalStop)
                            {
                                // Kill the route assignment and trigger the Cooldown
                                state.ResolvedRouteId = null;
                                state.IsAmbiguous = false;
                                state.UpcomingStopEtas.Clear();

                                // Default to 30 mins if property isn't on CachedRouteGeometry yet
                                var cooldownMins = 30; // We'll map this from your Route entity shortly!
                                state.CooldownEndsAtUtc = DateTime.UtcNow.AddMinutes(cooldownMins);

                                // WRITE-BEHIND: End the assignment safely
                                SafeFireAndForget(() => _repository.QueueActiveAssignmentCompletionAsync(capturedVehicleId));
                            }
                            else
                            {
                                // WRITE-BEHIND: Update sequence safely (capturedRouteId is safe here too)
                                SafeFireAndForget(() => _repository.QueueActiveAssignmentUpdateAsync(capturedVehicleId, capturedRouteId, capturedSequence));
                            }
                        }

                        // 7. CALCULATE ETAs FOR REMAINING STOPS
                        if (!isFinalStop && state.ResolvedRouteId.HasValue)
                        {
                            // P2 FIX: Computed ONCE here, passed to BroadcastStateAsync — not repeated there.
                            remainingStops = state.Direction == RouteDirection.Inbound
                                ? routeGeometry.Stops.Where(s => s.Sequence < state.LastPassedStopSequence).OrderByDescending(s => s.Sequence).ToList()
                                : routeGeometry.Stops.Where(s => s.Sequence > state.LastPassedStopSequence).OrderBy(s => s.Sequence).ToList();

                            if (remainingStops.Any())
                            {
                                // E1 FIX: Check polyline availability ONCE before the loop (was inside every iteration).
                                // This also fixes the bug where SnapToPolyline returned 0 for an empty polyline
                                // but the per-stop fallback only corrected distanceRemaining, not busDistanceOnLine.
                                bool hasPolyline = routeGeometry.PolylineShape.Any();

                                // P1 FIX: SnapToPolyline called ONCE here, result stored in busDistanceOnLine
                                // and passed to BroadcastStateAsync — not re-computed there.
                                busDistanceOnLine = hasPolyline
                                    ? GeoCalculator.SnapToPolyline(state.Latitude, state.Longitude, routeGeometry.PolylineShape)
                                    : 0;

                                foreach (var stop in remainingStops)
                                {
                                    double distanceRemaining = hasPolyline
                                        ? Math.Abs(stop.AccumulatedDistanceMeters - busDistanceOnLine)
                                        : GeoCalculator.GetDistanceMeters(state.Latitude, state.Longitude, stop.Latitude, stop.Longitude);

                                    // E6 FIX: Math.Max(MinSpeedMps, ...) prevents division by zero or near-zero speed.
                                    state.UpcomingStopEtas[stop.StopId] = DateTime.UtcNow.AddSeconds(
                                        distanceRemaining / Math.Max(MinSpeedMps, state.SmoothedSpeedMps ?? currentSpeedMps));
                                }
                            }
                        }
                    }
                }

                // 8. SAVE FAST CACHE
                await _cache.SetStateAsync(trackerId, state);

                // 9. BROADCAST LIVE DATA to the 3 SignalR groups
                // P1, P2, P3 FIX: Pre-computed routeGeometry, remainingStops, busDistanceOnLine are
                // passed in so BroadcastStateAsync never re-fetches from cache or re-runs geometry math.
                // NOTE: Kept as direct await (not fire-and-forget) until debugging confirms all broadcast
                // paths are stable. P7 (fire-and-forget) can be re-enabled once confirmed working.
                await BroadcastStateAsync(state, routeGeometry, remainingStops, busDistanceOnLine);

                return state;
            } // end try (A4 semaphore)
            finally
            {
                // A4 FIX: Always release the per-tracker lock, even if an exception occurred.
                // Without this, a single failed ping would permanently dead-lock that tracker.
                trackerLock.Release();
            }
        }

        /// <summary>
        /// Builds and fires all 3 SignalR broadcast payloads.
        /// Runs synchronously since all network calls inside are fire-and-forget.
        /// Pre-computed geometry values are injected to avoid redundant cache reads and CPU work.
        /// </summary>
        private async Task BroadcastStateAsync(
            VehicleLiveState state,
            CachedRouteGeometry? routeGeometry,  // P3: passed in, never re-fetched from cache
            List<CachedStop> remainingStops,      // P2: passed in, never re-computed
            double busDistanceOnLine)             // P1: passed in, SnapToPolyline not called again
        {
            // Scenario 3 (Live Map) should ALWAYS be broadcasted if we have coordinates, 
            // even if the route isn't resolved yet. This prevents a "dead map" during startup.
            var mapDto = new VehicleLiveMapDto
            {
                VehicleId = state.VehicleId,
                VehicleName = state.VehicleName,
                LicensePlate = state.LicensePlate,
                RouteName = state.RouteName ?? "Resolving Route...",
                Direction = state.Direction,
                Latitude = state.Latitude,
                Longitude = state.Longitude,
                Heading = state.Heading,
                SpeedKph = state.SmoothedSpeedMps.HasValue ? Math.Round(state.SmoothedSpeedMps.Value * 3.6, 1) : null
            };

            // Scenario 1 & 2 (and extra details for 3) require a resolved route
            if (state.ResolvedRouteId.HasValue && routeGeometry != null)
            {
                // P4 FIX: MinBy instead of .OrderBy().FirstOrDefault() for next-stop lookup.
                CachedStop? nextStop = state.Direction == RouteDirection.Inbound
                    ? routeGeometry.Stops.Where(s => s.Sequence < state.LastPassedStopSequence).MaxBy(s => s.Sequence)
                    : routeGeometry.Stops.Where(s => s.Sequence > state.LastPassedStopSequence).MinBy(s => s.Sequence);

                var nextStopEta = nextStop != null && state.UpcomingStopEtas.TryGetValue(nextStop.StopId, out var eta) ? (DateTime?)eta : null;

                // Update Map DTO with route details
                mapDto.NextStopName = nextStop?.StopName;
                mapDto.NextStopEtaUtc = nextStopEta;

                // ── Scenario 1: Route Bus List ───────────────────────────────────────────────
                var routeListDto = new RouteBusListDto
                {
                    VehicleId = state.VehicleId,
                    RouteId = state.ResolvedRouteId.Value,
                    VehicleName = state.VehicleName,
                    LicensePlate = state.LicensePlate,
                    RouteName = state.RouteName,
                    Direction = state.Direction,
                    NextStopName = nextStop?.StopName,
                    NextStopEtaUtc = nextStopEta
                };
                SafeFireAndForget(() => _broadcaster.BroadcastRouteUpdateAsync(routeListDto));

                // ── Scenario 2: Bus Stop Detail ──────────────────────────────────────────────
                if (remainingStops.Any())
                {
                    // E1/P5 FIX: Compute hasPolyline ONCE before the Select, not per-stop inside it.
                    bool hasPolyline = routeGeometry.PolylineShape.Any();

                    var upcomingStopDetails = remainingStops.Select(stop =>
                    {
                        state.UpcomingStopEtas.TryGetValue(stop.StopId, out var stopEta);

                        // P1 FIX: Uses busDistanceOnLine passed in — SnapToPolyline not called again.
                        double distMeters = hasPolyline
                            ? Math.Abs(stop.AccumulatedDistanceMeters - busDistanceOnLine)
                            : GeoCalculator.GetDistanceMeters(state.Latitude, state.Longitude, stop.Latitude, stop.Longitude);

                        string distText = distMeters >= 1000
                            ? $"{distMeters / 1000.0:F1} km"
                            : $"{(int)distMeters} m";

                        return new UpcomingStopDetailDto
                        {
                            StopId = stop.StopId,
                            Sequence = stop.Sequence,
                            StopName = stop.StopName,
                            EtaUtc = stopEta,
                            DistanceText = distText
                        };
                    }).ToList();

                    var textDto = new VehicleDetailTextDto
                    {
                        VehicleId = state.VehicleId,
                        VehicleName = state.VehicleName,
                        LicensePlate = state.LicensePlate,
                        RouteName = state.RouteName,
                        Direction = state.Direction,
                        NextStopName = nextStop?.StopName,
                        NextStopEtaUtc = nextStopEta,
                        UpcomingStops = upcomingStopDetails
                    };
                    SafeFireAndForget(() => _broadcaster.BroadcastVehicleTextUpdateAsync(textDto));
                }
            }

            // Always broadcast Map update (Scenario 3)
            await _broadcaster.BroadcastVehicleMapUpdateAsync(mapDto);
        }

        private async Task<VehicleLiveState> ResolveAmbiguousStateAsync(VehicleLiveState state)
        {
            Guid? bestMatchRoute = null;
            RouteDirection? bestMatchDirection = null;
            double shortestDistance = double.MaxValue;

            var geometriesList = new List<CachedRouteGeometry>();
            var cachedGeometries = await _cache.GetRouteGeometriesAsync(state.CandidateRouteIds);
            geometriesList.AddRange(cachedGeometries);

            var missingRouteIds = state.CandidateRouteIds.Except(geometriesList.Select(g => g.RouteId)).ToList();
            foreach (var missingRouteId in missingRouteIds)
            {
                var builtGeometry = await _repository.BuildRouteGeometryFromSqlAsync(missingRouteId);
                if (builtGeometry != null)
                {
                    builtGeometry.InitializePolylineDistances();
                    await _cache.SetRouteGeometryAsync(missingRouteId, builtGeometry);
                    geometriesList.Add(builtGeometry);
                }
            }

            foreach (var geometry in geometriesList)
            {
                var routeId = geometry.RouteId;

                // O(1) CPU Filter: If the bus isn't in this route's bounding box, skip math completely!
                if (!GeoCalculator.IsInsideBoundingBox(state.Latitude, state.Longitude, geometry.MinLat, geometry.MaxLat, geometry.MinLon, geometry.MaxLon))
                    continue;

                // P8 FIX: MinBy replaces .OrderBy().FirstOrDefault() — O(n) scan, no sort allocation.
                var closestStopTuple = geometry.Stops
                    .Select(s => (
                        Stop: s,
                        Distance: GeoCalculator.GetDistanceMeters(state.Latitude, state.Longitude, s.Latitude, s.Longitude)
                    ))
                    .MinBy(x => x.Distance);

                if (closestStopTuple.Stop != null)
                {
                    RouteDirection currentDirection = RouteDirection.Outbound;
                    bool headingMatches = true;

                    var nextStopsOutbound = geometry.Stops.Where(s => s.Sequence > closestStopTuple.Stop.Sequence).OrderBy(s => s.Sequence).ToList();
                    var nextStopsInbound = geometry.Stops.Where(s => s.Sequence < closestStopTuple.Stop.Sequence).OrderByDescending(s => s.Sequence).ToList();

                    if (state.Heading.HasValue)
                    {
                        double? outboundDiff = null;
                        if (nextStopsOutbound.Any())
                        {
                            var nextStop = nextStopsOutbound.First();
                            var routeBearing = GeoCalculator.GetBearing(closestStopTuple.Stop.Latitude, closestStopTuple.Stop.Longitude, nextStop.Latitude, nextStop.Longitude);
                            outboundDiff = Math.Abs(state.Heading.Value - routeBearing);
                            if (outboundDiff > 180) outboundDiff = 360 - outboundDiff;
                        }

                        double? inboundDiff = null;
                        if (nextStopsInbound.Any())
                        {
                            var nextStop = nextStopsInbound.First();
                            var routeBearing = GeoCalculator.GetBearing(closestStopTuple.Stop.Latitude, closestStopTuple.Stop.Longitude, nextStop.Latitude, nextStop.Longitude);
                            inboundDiff = Math.Abs(state.Heading.Value - routeBearing);
                            if (inboundDiff > 180) inboundDiff = 360 - inboundDiff;
                        }

                        if (outboundDiff.HasValue && inboundDiff.HasValue)
                        {
                            if (outboundDiff < inboundDiff)
                            {
                                currentDirection = RouteDirection.Outbound;
                                headingMatches = outboundDiff <= 90;
                            }
                            else
                            {
                                currentDirection = RouteDirection.Inbound;
                                headingMatches = inboundDiff <= 90;
                            }
                        }
                        else if (outboundDiff.HasValue)
                        {
                            currentDirection = RouteDirection.Outbound;
                            headingMatches = outboundDiff <= 90;
                        }
                        else if (inboundDiff.HasValue)
                        {
                            currentDirection = RouteDirection.Inbound;
                            headingMatches = inboundDiff <= 90;
                        }
                        else
                        {
                            headingMatches = false;
                        }
                    }

                    if (headingMatches && closestStopTuple.Distance < shortestDistance)
                    {
                        shortestDistance = closestStopTuple.Distance;
                        bestMatchRoute = routeId;
                        bestMatchDirection = currentDirection;
                    }
                }
            }

            if (bestMatchRoute.HasValue && bestMatchDirection.HasValue)
            {
                state.ResolvedRouteId = bestMatchRoute.Value;
                state.Direction = bestMatchDirection.Value;
                state.IsAmbiguous = false;
                state.CandidateRouteIds.Clear();

                // WRITE-BEHIND: We finally know the route and direction, save it to SQL safely in the background!
                SafeFireAndForget(() => _repository.QueueNewActiveAssignmentAsync(state.VehicleId, bestMatchRoute.Value, bestMatchDirection.Value));
            }

            return state;
        }

        private void SafeFireAndForget(Func<Task> action)
        {
            Task.Run(async () =>
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    // Temporarily logging to diagnose broadcast failures.
                    // Remove once confirmed stable.
                    Console.WriteLine($"[FireAndForget ERROR] {ex.GetType().Name}: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            });
        }
    }
}