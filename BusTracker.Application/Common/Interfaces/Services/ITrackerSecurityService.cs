namespace BusTracker.Application.Common.Interfaces.Services
{
    public interface ITrackerSecurityService
    {
        bool IsSignatureValid(string trackerId, string rawPayload, string providedSignature);
    }
}
