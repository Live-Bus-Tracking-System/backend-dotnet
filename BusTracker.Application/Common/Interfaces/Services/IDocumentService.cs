namespace BusTracker.Application.Common.Interfaces.Services
{

    public interface IDocumentService
    {
        string EncryptUrl(string rawUrl);

        string DecryptUrl(string encryptedUrl);
    }
}
