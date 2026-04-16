using BusTracker.Domain.Common;

namespace BusTracker.Domain.Events.Organisations
{
    public record OrganisationCreatedDomainEvent(Guid OrganisationId, string Name, string Email, string PhoneNumber, string CreatedByUserId) : IDomainEvent;

    public record OrganisationUpdatedDomainEvent(Guid OrganisationId, string Email, string Name) : IDomainEvent;

    public record OrganisationActivatedDomainEvent(Guid OrganisationId, string Email, string Name) : IDomainEvent;

    public record OrganisationSuspendedDomainEvent(Guid OrganisationId, string Email, string Name, string? Reason) : IDomainEvent;

    public record OrganisationDeletedDomainEvent(Guid OrganisationId, string Email, string Name) : IDomainEvent;
}
