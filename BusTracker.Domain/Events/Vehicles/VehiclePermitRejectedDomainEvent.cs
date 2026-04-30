using BusTracker.Domain.Common;

namespace BusTracker.Domain.Events.Vehicles
{
    public record VehiclePermitRejectedDomainEvent(Guid PermitId, Guid VehicleId, Guid OrganizationId, string RejectedBy, string RejectionReason) : IDomainEvent;
}
