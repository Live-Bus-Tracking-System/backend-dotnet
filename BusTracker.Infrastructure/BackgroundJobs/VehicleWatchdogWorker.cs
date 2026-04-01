using BusTracker.Application.Common.Interfaces;
using BusTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusTracker.Infrastructure.BackgroundJobs
{
    public class VehicleWatchdogWorker : BackgroundService
    {
        private readonly ILogger<VehicleWatchdogWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public VehicleWatchdogWorker(
            ILogger<VehicleWatchdogWorker> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Vehicle Watchdog Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessStaleVehiclesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while running the vehicle watchdog sweep.");
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        private async Task ProcessStaleVehiclesAsync(CancellationToken stoppingToken)
        {
            // Create a fresh scope per sweep so Scoped services (like IVehicleStateCache) are resolved safely
            using var scope = _scopeFactory.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IVehicleStateCache>();

            var allActiveStates = await cache.GetAllActiveVehiclesAsync();
            var now = DateTime.UtcNow;

            foreach (var (trackerId, state) in allActiveStates)
            {
                if (state.IsHardOffline) continue;

                var timeSinceLastPing = now - state.TimestampUtc;

                if (timeSinceLastPing.TotalMinutes >= 30)
                {
                    _logger.LogWarning($"Vehicle {state.VehicleId} (Tracker: {trackerId}) has been offline for {timeSinceLastPing.TotalMinutes:F1} minutes. Marking Hard Offline.");

                    state.IsHardOffline = true;
                    await cache.SetStateAsync(trackerId, state);

                    await CloseActiveAssignmentAsync(state.VehicleId, stoppingToken);
                }
            }
        }

        private async Task CloseActiveAssignmentAsync(Guid VehicleId, CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var assignment = await dbContext.ActiveAssignments
                .FirstOrDefaultAsync(a => a.VehicleId == VehicleId && a.IsCompleted == false, stoppingToken);

            if (assignment != null && !assignment.IsCompleted)
            {
                assignment.IsCompleted = true;
                assignment.EndTimeUtc = DateTime.UtcNow;

                await dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation($"Successfully closed ActiveAssignment of vehicle: {VehicleId} ,due to 30-minute timeout.");
            }
        }
    }
}