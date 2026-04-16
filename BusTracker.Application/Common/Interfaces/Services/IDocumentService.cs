namespace BusTracker.Application.Common.Interfaces.Services
{

    public interface IDocumentService
    {
        string EncryptUrl(string rawUrl);

        string DecryptUrl(string encryptedUrl);

        string GenerateAccessToken(Guid documentId, string requestorUserId, TimeSpan? expiresIn = null);

        DocumentAccessResult ValidateAccessToken(string token);
    }

    public record DocumentAccessResult(bool IsValid, Guid DocumentId, string? DecryptedUrl, string? Error);
}
