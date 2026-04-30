namespace BusTracker.Application.Features.Permits.DTOs
{
    public record PendingPermitDto(
        Guid PermitId,
        Guid VehicleId,
        Guid OrganizationId,
        string OrganizationName,
        string LicensePlate,
        string? VehicleName,
        DateTime SubmittedAtUtc
    );

    public record PendingPermitDetailDto(
        Guid PermitId,
        Guid VehicleId,
        Guid OrganizationId,
        string OrganizationName,
        string LicensePlate,
        string TrackerId,
        string? VehicleName,
        int? Capacity,
        string? RegistrationNotes,
        DateTime SubmittedAtUtc,
        IEnumerable<PermitDocumentDto> Documents
    );

    public record PermitDocumentDto(
        Guid DocumentId,
        string DocumentType,
        string? DocumentNumber,
        string? IssuedBy,
        DateOnly? IssuedAtDate,
        DateOnly? ExpiresAtDate,
        bool IsVerified,
        string? VerifiedBy,
        DateTime? VerifiedAtUtc
    );
}
