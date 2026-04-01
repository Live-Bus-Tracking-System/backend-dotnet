using BusTracker.Application.Tracking.Models;

namespace BusTracker.Application.Common.Interfaces.Services
{
    public interface ILocationProcessorService
    {
        Task<VehicleLiveState> ProcessPingAsync(string trackerId, LocationPingDto ping);
    }
}
