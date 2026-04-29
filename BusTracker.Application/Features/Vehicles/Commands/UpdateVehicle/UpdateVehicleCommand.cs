using BusTracker.Application.Features.Vehicles.DTOs;
using MediatR;

namespace BusTracker.Application.Features.Vehicles.Commands.UpdateVehicle
{
    public record UpdateVehicleCommand(
        Guid VehicleId,

        string? LicensePlate,
        string? TrackerId,
        string? Name,
        int? Capacity,

        string? RegistrationCertificateObjectKey,
        string? RegistrationCertificateNumber,
        string? RegistrationCertIssuedBy,
        DateOnly? RegistrationCertIssuedAt,
        DateOnly? RegistrationCertExpiresAt,

        string? PermitCertificateObjectKey,
        string? PermitCertificateNumber,
        string? PermitCertIssuedBy,
        DateOnly? PermitCertIssuedAt,
        DateOnly? PermitCertExpiresAt,

        string? IntendedRouteName,
        string? StartStopName,
        string? EndStopName,
        string? AdditionalNotes,

        string? PermitNumber
    ) : IRequest<VehicleDto>;
}
