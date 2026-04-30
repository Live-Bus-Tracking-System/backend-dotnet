using BusTracker.Domain.Common;

namespace BusTracker.Domain.Events.Vehicles
{
    public record VehiclePermitApprovedDomainEvent(Guid PermitId, Guid VehicleId, Guid OrganizationId, string ApprovedBy) : IDomainEvent;
}
