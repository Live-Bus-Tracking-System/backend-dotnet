using System;
using System.Threading.Tasks;

namespace BusTracker.Application.Common.Interfaces
{
    public interface IOrgDeletionIntentCache
    {
        Task StoreOtpIntentAsync(string intentId, OrgDeletionOtpIntent intent, TimeSpan ttl);
        Task<OrgDeletionOtpIntent?> GetOtpIntentAsync(string intentId);
        Task RemoveOtpIntentAsync(string intentId);

        Task StoreConfirmTokenAsync(string confirmToken, OrgDeletionConfirmIntent intent, TimeSpan ttl);
        Task<OrgDeletionConfirmIntent?> GetConfirmIntentAsync(string confirmToken);
        Task RemoveConfirmIntentAsync(string confirmToken);
    }

    public record OrgDeletionOtpIntent(Guid OrgId, string UserId, string OtpHash);
    public record OrgDeletionConfirmIntent(Guid OrgId, string UserId);
}
