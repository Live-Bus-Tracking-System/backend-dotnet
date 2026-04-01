using System.Threading.Channels;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Tracking.Models;

namespace BusTracker.Infrastructure.Services
{
    public class TrackingEventChannel : ITrackingEventChannel
    {
        private readonly Channel<TrackingEvent> _channel;

        public TrackingEventChannel()
        {
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _channel = Channel.CreateBounded<TrackingEvent>(options);
        }

        public bool TryWrite(TrackingEvent trackingEvent)
        {
            return _channel.Writer.TryWrite(trackingEvent);
        }

        public IAsyncEnumerable<TrackingEvent> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
