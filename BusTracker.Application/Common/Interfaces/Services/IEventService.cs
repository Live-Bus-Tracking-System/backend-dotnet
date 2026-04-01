using System.Threading;
using System.Threading.Tasks;

namespace BusTracker.Application.Common.Interfaces.Services
{
    public interface IEventService
    {
        Task EmitAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default);
    }
}
