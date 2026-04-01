using System.Threading;
using System.Threading.Tasks;

namespace BusTracker.Application.Common.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
    }
}
