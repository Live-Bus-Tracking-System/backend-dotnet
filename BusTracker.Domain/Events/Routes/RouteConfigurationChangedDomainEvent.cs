using BusTracker.Domain.Common;

namespace BusTracker.Domain.Events.Routes
{
    public record RouteConfigurationChangedDomainEvent(Guid RouteId, string RouteNumber, Guid? OrganizationId) : IDomainEvent;
}
