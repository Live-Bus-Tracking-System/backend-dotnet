using BusTracker.Application.Features.Vehicles.DTOs;
using MediatR;

namespace BusTracker.Application.Features.Vehicles.Commands.RegisterVehicle
{
    public record RegisterVehicleCommand(
        string LicensePlate,
        string TrackerId,
        string? Name,
        int? Capacity,

        string RegistrationCertificateObjectKey,
        string PermitCertificateObjectKey,

        string? RegistrationCertificateNumber,
        string? PermitCertificateNumber,
        string? RegistrationCertIssuedBy,
        string? PermitCertIssuedBy,
        DateOnly? RegistrationCertIssuedAt,
        DateOnly? RegistrationCertExpiresAt,
        DateOnly? PermitCertIssuedAt,
        DateOnly? PermitCertExpiresAt,

        string? IntendedRouteName,
        string? StartStopName,
        string? EndStopName,
        string? AdditionalNotes
    ) : IRequest<VehicleRegistrationSubmittedDto>;
}
