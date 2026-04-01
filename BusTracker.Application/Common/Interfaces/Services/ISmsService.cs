using System.Threading;
using System.Threading.Tasks;

namespace BusTracker.Application.Common.Interfaces.Services
{
    public interface ISmsService
    {
        Task SendSmsAsync(string toPhoneNumber, string message, CancellationToken cancellationToken = default);
    }
}
