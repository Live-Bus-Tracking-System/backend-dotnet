namespace BusTracker.Application.Features.Vehicles.DTOs
{
    public record VehicleSummaryDto(
        Guid Id,
        Guid OrganisationId,
        string LicensePlate,
        string? Name,
        int? Capacity,
        bool IsActive,
        DateTime CreatedAtUtc
    );

    public record VehicleDto(
        Guid Id,
        Guid OrganisationId,
        string TrackerId,
        string LicensePlate,
        string? Name,
        int? Capacity,
        bool IsActive,
        DateTime CreatedAtUtc,
        string? CreatedBy,
        DateTime? LastModifiedAtUtc,
        string? LastModifiedBy
    );

    /// <summary>
    /// Returned when a PublicTransit org submits a vehicle registration.
    /// Includes the pending permit and submitted compliance document IDs.
    /// </summary>
    public record VehicleRegistrationSubmittedDto(
        Guid VehicleId,
        string LicensePlate,
        bool IsActive,
        Guid? PermitId,
        string? PermitStatus,
        IEnumerable<SubmittedDocumentDto> Documents,
        string Message
    );

    public record SubmittedDocumentDto(
        Guid DocumentId,
        string DocumentType,
        string? ExtractedCertificateNumber,
        string? ExtractedIssuedBy,
        DateOnly? ExtractedIssuedAt,
        DateOnly? ExtractedExpiresAt
    );
}
