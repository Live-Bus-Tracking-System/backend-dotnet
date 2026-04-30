using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Features.Permits.DTOs;
using BusTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Permits.Queries.GetPendingPermitDetail
{
    public class GetPendingPermitDetailQueryHandler : IRequestHandler<GetPendingPermitDetailQuery, PendingPermitDetailDto>
    {
        private readonly IApplicationDbContext _db;

        public GetPendingPermitDetailQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PendingPermitDetailDto> Handle(GetPendingPermitDetailQuery request, CancellationToken cancellationToken)
        {
            var permit = await _db.VehiclePermits
                .AsNoTracking()
                .Include(p => p.Vehicle)
                .Include(p => p.Organization)
                .FirstOrDefaultAsync(p => p.Id == request.PermitId && !p.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("VehiclePermit", request.PermitId);

            var documents = await _db.ComplianceDocuments
                .AsNoTracking()
                .Where(d => !d.IsDeleted && d.EntityType == ComplianceDocumentEntityType.Vehicle && d.EntityId == permit.VehicleId)
                .Select(d => new PermitDocumentDto(
                    d.Id,
                    d.DocumentType.ToString(),
                    d.DocumentNumber,
                    d.IssuedBy,
                    d.IssuedAtDate,
                    d.ExpiresAtDate,
                    d.IsVerified,
                    d.VerifiedBy,
                    d.VerifiedAtUtc
                ))
                .ToListAsync(cancellationToken);

            return new PendingPermitDetailDto(
                permit.Id,
                permit.VehicleId,
                permit.OrganizationId,
                permit.Organization!.Name,
                permit.Vehicle!.LicensePlate,
                permit.Vehicle.TrackerId,
                permit.Vehicle.Name,
                permit.Vehicle.Capacity,
                permit.Vehicle.RegistrationNotes,
                permit.CreatedAtUtc,
                documents
            );
        }
    }
}
