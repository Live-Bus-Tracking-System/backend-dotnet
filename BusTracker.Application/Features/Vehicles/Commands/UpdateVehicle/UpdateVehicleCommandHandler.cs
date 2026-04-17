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

namespace BusTracker.Application.Features.Vehicles.Commands.UpdateVehicle
{
    public class UpdateVehicleCommandHandler : IRequestHandler<UpdateVehicleCommand, VehicleDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IVehicleStateCache _cache;
        private readonly IDocumentService _documentService;
        private readonly IDocumentIntelligenceService _documentIntelligence;
        private readonly IValidator<UpdateVehicleCommand> _validator;

        public UpdateVehicleCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IVehicleStateCache cache,
            IDocumentService documentService,
            IDocumentIntelligenceService documentIntelligence,
            IValidator<UpdateVehicleCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _cache = cache;
            _documentService = documentService;
            _documentIntelligence = documentIntelligence;
            _validator = validator;
        }

        public async Task<VehicleDto> Handle(UpdateVehicleCommand request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request, cancellationToken);
            if (!validation.IsValid)
                throw new CustomValidationException(validation.Errors);

            // Load vehicle with related compliance docs and permits
            var vehicle = await _db.Vehicles
                .Include(v => v.Permits)
                .FirstOrDefaultAsync(v => v.Id == request.VehicleId && !v.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Vehicle", request.VehicleId);

            // Org-scoping: non-SuperAdmin can only update their own org's vehicles
            if (!_currentUser.IsSuperAdmin && _currentUser.OrganisationId != vehicle.OrganizationId)
                throw new ForbiddenException();

            var oldTrackerId = vehicle.TrackerId;
            var trackerIdChanged = false;
            var complianceDocsRenewed = false;

            // ── Group 1: Core field patches ────────────────────────────────────────
            if (request.LicensePlate is not null)
            {
                var normalizedPlate = request.LicensePlate.Trim().ToUpperInvariant();
                if (normalizedPlate != vehicle.LicensePlate)
                {
                    var plateConflict = await _db.Vehicles
                        .AnyAsync(v => !v.IsDeleted && v.LicensePlate == normalizedPlate && v.Id != vehicle.Id, cancellationToken);
                    if (plateConflict)
                        throw new CustomValidationException([new("LicensePlate","A vehicle with this license plate already exists in an organisation.")]);

                    vehicle.LicensePlate = normalizedPlate;
                }
            }

            if (request.TrackerId is not null)
            {
                var normalizedTrackerId = request.TrackerId.Trim();
                if (normalizedTrackerId != vehicle.TrackerId)
                {
                    var trackerConflict = await _db.Vehicles
                        .AnyAsync(v => !v.IsDeleted && v.TrackerId == normalizedTrackerId && v.Id != vehicle.Id, cancellationToken);
                    if (trackerConflict)
                        throw new CustomValidationException([new("TrackerId",
                            "A vehicle with this tracker ID already exists.")]);

                    vehicle.TrackerId = normalizedTrackerId;
                    trackerIdChanged = true;
                }
            }

            if (request.Name is not null)
                vehicle.Name = request.Name.Trim();

            if (request.Capacity is not null)
                vehicle.Capacity = request.Capacity;

            // ── Group 2: Registration certificate renewal ──────────────────────────
            if (request.RegistrationCertificateUrl is not null)
            {
                await RenewComplianceDocumentAsync(
                    vehicle.Id,
                    ComplianceDocumentType.VehicleRegistration,
                    request.RegistrationCertificateUrl,
                    request.RegistrationCertificateNumber,
                    request.RegistrationCertIssuedBy,
                    request.RegistrationCertIssuedAt,
                    request.RegistrationCertExpiresAt,
                    cancellationToken);

                complianceDocsRenewed = true;
            }

            // ── Group 3: Permit certificate renewal ────────────────────────────────
            if (request.PermitCertificateUrl is not null)
            {
                await RenewComplianceDocumentAsync(
                    vehicle.Id,
                    ComplianceDocumentType.RoutePermitDoc,
                    request.PermitCertificateUrl,
                    request.PermitCertificateNumber,
                    request.PermitCertIssuedBy,
                    request.PermitCertIssuedAt,
                    request.PermitCertExpiresAt,
                    cancellationToken);

                complianceDocsRenewed = true;
            }

            // When any cert is renewed, reset the active/pending permit back to Pending
            // so a SuperAdmin must re-review before the vehicle can be considered compliant
            if (complianceDocsRenewed)
            {
                var activePermit = vehicle.Permits
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.CreatedAtUtc)
                    .FirstOrDefault();

                if (activePermit is not null)
                    activePermit.PermitStatus = PermitStatus.Pending;

                vehicle.IsActive = false;
            }

            // ── Group 4: Registration notes rebuild ────────────────────────────────
            var notesGroupSubmitted =
                request.IntendedRouteName is not null ||
                request.StartStopName is not null     ||
                request.EndStopName is not null       ||
                request.AdditionalNotes is not null;

            if (notesGroupSubmitted)
            {
                // Parse existing notes so we can patch individual segments
                var existing = ParseRegistrationNotes(vehicle.RegistrationNotes);

                var route   = request.IntendedRouteName ?? existing.route;
                var start   = request.StartStopName     ?? existing.start;
                var end     = request.EndStopName       ?? existing.end;
                var notes   = request.AdditionalNotes   ?? existing.notes;

                vehicle.RegistrationNotes = BuildRegistrationNotes(route, start, end, notes);
            }

            // ── Group 5: Permit number patch ───────────────────────────────────────
            if (request.PermitNumber is not null)
            {
                var permit = vehicle.Permits
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.CreatedAtUtc)
                    .FirstOrDefault();

                if (permit is not null)
                    permit.PermitNumber = request.PermitNumber.Trim();
            }

            // ── Domain Event ───────────────────────────────────────────────────────
            vehicle.AddDomainEvent(new VehicleUpdatedDomainEvent(
                vehicle.Id, vehicle.LicensePlate, vehicle.Name));

            await _db.SaveChangesAsync(cancellationToken);

            // ── Cache update (silent, after DB commit) ─────────────────────────────
            if (complianceDocsRenewed)
                await _cache.DeleteTrackerStateAsync(oldTrackerId);
            else if (trackerIdChanged)
                await _cache.MigrateTrackerStateAsync(oldTrackerId, vehicle.TrackerId);

            return new VehicleDto(
                vehicle.Id, vehicle.OrganizationId, vehicle.TrackerId, vehicle.LicensePlate,
                vehicle.Name, vehicle.Capacity, vehicle.IsActive,
                vehicle.CreatedAtUtc, vehicle.CreatedBy, vehicle.LastModifiedAtUtc, vehicle.LastModifiedBy);
        }

        // ── Helpers ────────────────────────────────────────────────────────────────

        private async Task RenewComplianceDocumentAsync(
            Guid vehicleId,
            ComplianceDocumentType docType,
            string rawUrl,
            string? certNumber,
            string? issuedBy,
            DateOnly? issuedAt,
            DateOnly? expiresAt,
            CancellationToken cancellationToken)
        {
            var encryptedUrl = _documentService.EncryptUrl(rawUrl);
            var extraction   = await _documentIntelligence.ExtractAsync(rawUrl, cancellationToken);

            var existing = await _db.ComplianceDocuments
                .FirstOrDefaultAsync(d => d.EntityId == vehicleId
                    && d.EntityType == ComplianceDocumentEntityType.Vehicle
                    && d.DocumentType == docType
                    && !d.IsDeleted, cancellationToken);

            if (existing is not null)
            {
                // Update in-place
                existing.DocumentUrl    = encryptedUrl;
                existing.DocumentNumber = certNumber ?? extraction?.CertificateNumber ?? existing.DocumentNumber;
                existing.IssuedBy       = issuedBy   ?? extraction?.IssuedBy          ?? existing.IssuedBy;
                existing.IssuedAtDate   = issuedAt   ?? extraction?.IssuedAt          ?? existing.IssuedAtDate;
                existing.ExpiresAtDate  = expiresAt  ?? extraction?.ExpiresAt         ?? existing.ExpiresAtDate;
                existing.IsVerified     = false;             // Force re-verification
                existing.VerifiedBy     = null;
                existing.VerifiedAtUtc  = null;
            }
            else
            {
                // First-time submission (e.g. org adding cert retroactively)
                _db.ComplianceDocuments.Add(new ComplianceDocument
                {
                    EntityType     = ComplianceDocumentEntityType.Vehicle,
                    EntityId       = vehicleId,
                    DocumentType   = docType,
                    DocumentUrl    = encryptedUrl,
                    DocumentNumber = certNumber ?? extraction?.CertificateNumber,
                    IssuedBy       = issuedBy   ?? extraction?.IssuedBy,
                    IssuedAtDate   = issuedAt   ?? extraction?.IssuedAt,
                    ExpiresAtDate  = expiresAt  ?? extraction?.ExpiresAt,
                    IsVerified     = false,
                });
            }
        }

        private static string? BuildRegistrationNotes(
            string? route, string? start, string? end, string? notes)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(route))  sb.Append($"Route: {route}");
            if (!string.IsNullOrWhiteSpace(start))  sb.Append($" | Start: {start}");
            if (!string.IsNullOrWhiteSpace(end))    sb.Append($" | End: {end}");
            if (!string.IsNullOrWhiteSpace(notes))  sb.Append($" | Notes: {notes}");
            return sb.Length > 0 ? sb.ToString() : null;
        }

        private static (string? route, string? start, string? end, string? notes)
            ParseRegistrationNotes(string? existing)
        {
            if (string.IsNullOrWhiteSpace(existing))
                return (null, null, null, null);

            string? route = null, start = null, end = null, notes = null;
            foreach (var segment in existing.Split('|', StringSplitOptions.TrimEntries))
            {
                if (segment.StartsWith("Route: ",  StringComparison.OrdinalIgnoreCase)) route = segment[7..];
                else if (segment.StartsWith("Start: ", StringComparison.OrdinalIgnoreCase)) start = segment[7..];
                else if (segment.StartsWith("End: ",   StringComparison.OrdinalIgnoreCase)) end   = segment[5..];
                else if (segment.StartsWith("Notes: ", StringComparison.OrdinalIgnoreCase)) notes = segment[7..];
            }
            return (route, start, end, notes);
        }
    }
}
