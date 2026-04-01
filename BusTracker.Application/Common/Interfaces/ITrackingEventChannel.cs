using BusTracker.Application.Tracking.Models;

namespace BusTracker.Application.Common.Interfaces
{
    public interface ITrackingEventChannel
    {
        bool TryWrite(TrackingEvent trackingEvent);
        IAsyncEnumerable<TrackingEvent> ReadAllAsync(CancellationToken cancellationToken);
    }
}
