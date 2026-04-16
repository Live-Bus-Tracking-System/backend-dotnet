using BusTracker.Domain.Enums;

namespace BusTracker.Application.Features.Organisations.DTOs
{
    public record OrganisationSummaryDto(
        Guid Id,
        string Name,
        string NormalizedEmail,
        string NormalizedPhoneNumber,
        OrganizationType Type,
        OrganisationStatus Status,
        DateTime CreatedAtUtc
    );

    public record OrganisationDto(
        Guid Id,
        string Name,
        string NormalizedEmail,
        string NormalizedPhoneNumber,
        OrganizationType Type,
        OrganisationStatus Status,
        bool IsOperational,
        DateTime CreatedAtUtc,
        string? CreatedBy,
        DateTime? LastModifiedAtUtc,
        string? LastModifiedBy
    );
}
