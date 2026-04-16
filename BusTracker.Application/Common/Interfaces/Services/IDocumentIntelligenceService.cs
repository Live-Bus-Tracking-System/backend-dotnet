namespace BusTracker.Application.Common.Interfaces.Services
{
    public interface IDocumentIntelligenceService
    {
        Task<CertificateExtractionResult?> ExtractAsync(string documentUrl, CancellationToken cancellationToken = default);
    }

    public record CertificateExtractionResult(
        string? CertificateNumber,
        string? IssuedBy,
        DateOnly? IssuedAt,
        DateOnly? ExpiresAt,
        double? ConfidenceScore
    );
}
