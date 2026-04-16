using BusTracker.Domain.Common;

namespace BusTracker.Domain.Events.Vehicles
{
    public record VehicleRegisteredDomainEvent(
        Guid VehicleId,
        string LicensePlate,
        string? Name,
        Guid OrganisationId,
        string CreatedByUserId) : IDomainEvent;

    public record VehicleUpdatedDomainEvent(
        Guid VehicleId,
        string LicensePlate,
        string? Name) : IDomainEvent;

    public record VehicleActivatedDomainEvent(
        Guid VehicleId,
        string LicensePlate) : IDomainEvent;

    public record VehicleDeactivatedDomainEvent(
        Guid VehicleId,
        string LicensePlate) : IDomainEvent;

    public record VehicleDeletedDomainEvent(
        Guid VehicleId,
        string LicensePlate) : IDomainEvent;
}
