using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Application.Features.Vehicles.DTOs;
using BusTracker.Domain.Entities;
using BusTracker.Domain.Enums;
using BusTracker.Domain.Events.Vehicles;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace BusTracker.Application.Features.Vehicles.Commands.RegisterVehicle
{
    public class RegisterVehicleCommandHandler : IRequestHandler<RegisterVehicleCommand, VehicleRegistrationSubmittedDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<RegisterVehicleCommand> _validator;
        private readonly IDocumentService _documentService;
        private readonly IDocumentIntelligenceService _documentIntelligence;

        public RegisterVehicleCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<RegisterVehicleCommand> validator,
            IDocumentService documentService,
            IDocumentIntelligenceService documentIntelligence)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
            _documentService = documentService;
            _documentIntelligence = documentIntelligence;
        }

        public async Task<VehicleRegistrationSubmittedDto> Handle(RegisterVehicleCommand request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            if (_currentUser.OrganisationId is null)
                throw new ForbiddenException();

            var orgId = _currentUser.OrganisationId.Value;

            // Load the org to determine registration flow by type
            var org = await _db.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orgId && !o.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Organisation", orgId);

            var normalizedPlate    = request.LicensePlate.Trim().ToUpperInvariant();
            var normalizedTrackerId = request.TrackerId.Trim();

            // Uniqueness check
            var conflict = await _db.Vehicles
                .AsNoTracking()
                .Where(v => !v.IsDeleted && (v.LicensePlate == normalizedPlate || v.TrackerId == normalizedTrackerId))
                .Select(v => new { v.LicensePlate, v.TrackerId })
                .FirstOrDefaultAsync(cancellationToken);

            if (conflict is not null)
            {
                var errors = new List<FluentValidation.Results.ValidationFailure>(2);
                if (conflict.LicensePlate == normalizedPlate)
                    errors.Add(new("LicensePlate", "A vehicle with this license plate already exists in an organisation."));
                if (conflict.TrackerId == normalizedTrackerId)
                    errors.Add(new("TrackerId", "A vehicle with this tracker ID already exists in an organisation."));
                throw new CustomValidationException(errors);
            }

            var requiresVerification = org.Type == OrganizationType.PublicTransit;

            // Build the RegistrationNotes formatted string
            var notesBuilder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(request.IntendedRouteName))
                notesBuilder.Append($"Route: {request.IntendedRouteName}");
            if (!string.IsNullOrWhiteSpace(request.StartStopName))
                notesBuilder.Append($" | Start: {request.StartStopName}");
            if (!string.IsNullOrWhiteSpace(request.EndStopName))
                notesBuilder.Append($" | End: {request.EndStopName}");
            if (!string.IsNullOrWhiteSpace(request.AdditionalNotes))
                notesBuilder.Append($" | Notes: {request.AdditionalNotes}");

            var vehicle = new Vehicle
            {
                OrganizationId    = orgId,
                LicensePlate      = normalizedPlate,
                TrackerId         = normalizedTrackerId,
                Name              = request.Name?.Trim(),
                Capacity          = request.Capacity,
                IsActive          = !requiresVerification,
                RegistrationNotes = notesBuilder.Length > 0 ? notesBuilder.ToString() : null,
            };

            vehicle.AddDomainEvent(new VehicleRegisteredDomainEvent(vehicle.Id, vehicle.LicensePlate, vehicle.Name, orgId, _currentUser.UserId!));

            _db.Vehicles.Add(vehicle);

            VehiclePermit? permit = null;
            var submittedDocs = new List<SubmittedDocumentDto>();

            if (requiresVerification)
            {
                // ── Step 1: Attempt AI extraction on both certificates ────────────────
                var regTask = _documentIntelligence.ExtractAsync(request.RegistrationCertificateUrl, cancellationToken);
                var permitTask = _documentIntelligence.ExtractAsync(request.PermitCertificateUrl, cancellationToken);
                await Task.WhenAll(regTask, permitTask);
                var regExtraction = await regTask;
                var permitExtraction = await permitTask;

                // ── Step 2: Encrypt certificate URLs before storing ──────────────────
                var encryptedRegUrl    = _documentService.EncryptUrl(request.RegistrationCertificateUrl);
                var encryptedPermitUrl = _documentService.EncryptUrl(request.PermitCertificateUrl);

                // ── Step 3: Create VehiclePermit (Pending status) ────────────────────
                permit = new VehiclePermit
                {
                    VehicleId      = vehicle.Id,
                    OrganizationId = orgId,
                    PermitStatus   = PermitStatus.Pending,
                    Notes          = $"Submitted by {_currentUser.UserId} on {DateTime.UtcNow:O}",
                };
                _db.VehiclePermits.Add(permit);

                // ── Step 4: Create 2 ComplianceDocuments ─────────────────────────────
                var regDoc = new ComplianceDocument
                {
                    EntityType      = ComplianceDocumentEntityType.Vehicle,
                    EntityId        = vehicle.Id,
                    DocumentType    = ComplianceDocumentType.VehicleRegistration,
                    DocumentUrl     = encryptedRegUrl,
                    DocumentNumber  = request.RegistrationCertificateNumber ?? regExtraction?.CertificateNumber,
                    IssuedBy        = request.RegistrationCertIssuedBy      ?? regExtraction?.IssuedBy,
                    IssuedAtDate    = request.RegistrationCertIssuedAt       ?? regExtraction?.IssuedAt,
                    ExpiresAtDate   = request.RegistrationCertExpiresAt      ?? regExtraction?.ExpiresAt,
                    IsVerified      = false,
                };

                var permitDoc = new ComplianceDocument
                {
                    EntityType      = ComplianceDocumentEntityType.Vehicle,
                    EntityId        = vehicle.Id,
                    DocumentType    = ComplianceDocumentType.RoutePermitDoc,
                    DocumentUrl     = encryptedPermitUrl,
                    DocumentNumber  = request.PermitCertificateNumber ?? permitExtraction?.CertificateNumber,
                    IssuedBy        = request.PermitCertIssuedBy      ?? permitExtraction?.IssuedBy,
                    IssuedAtDate    = request.PermitCertIssuedAt       ?? permitExtraction?.IssuedAt,
                    ExpiresAtDate   = request.PermitCertExpiresAt      ?? permitExtraction?.ExpiresAt,
                    IsVerified      = false,
                };

                _db.ComplianceDocuments.Add(regDoc);
                _db.ComplianceDocuments.Add(permitDoc);

                submittedDocs.Add(new SubmittedDocumentDto(
                    regDoc.Id, nameof(ComplianceDocumentType.VehicleRegistration),
                    regDoc.DocumentNumber, regDoc.IssuedBy, regDoc.IssuedAtDate, regDoc.ExpiresAtDate));

                submittedDocs.Add(new SubmittedDocumentDto(
                    permitDoc.Id, nameof(ComplianceDocumentType.RoutePermitDoc),
                    permitDoc.DocumentNumber, permitDoc.IssuedBy, permitDoc.IssuedAtDate, permitDoc.ExpiresAtDate));
            }

            await _db.SaveChangesAsync(cancellationToken);

            var message = requiresVerification
                ? "Vehicle registration submitted successfully. It is pending SuperAdmin review before activation."
                : "Vehicle registered and activated successfully.";

            return new VehicleRegistrationSubmittedDto(
                VehicleId:    vehicle.Id,
                LicensePlate: vehicle.LicensePlate,
                IsActive:     vehicle.IsActive,
                PermitId:     permit?.Id,
                PermitStatus: permit?.PermitStatus.ToString(),
                Documents:    submittedDocs,
                Message:      message);
        }
    }
}
