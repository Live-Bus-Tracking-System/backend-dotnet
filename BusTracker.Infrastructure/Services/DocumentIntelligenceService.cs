using BusTracker.Application.Common.Interfaces.Services;

namespace BusTracker.Infrastructure.Services
{
    public class DocumentIntelligenceService : IDocumentIntelligenceService
    {
        public Task<CertificateExtractionResult?> ExtractAsync(string documentUrl, CancellationToken cancellationToken = default)
        {
            // MVP: no-op
            return Task.FromResult<CertificateExtractionResult?>(null);
        }
    }
}
