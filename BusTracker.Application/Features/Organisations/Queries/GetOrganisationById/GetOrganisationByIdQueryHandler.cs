using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Features.Organisations.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Organisations.Queries.GetOrganisationById
{
    public class GetOrganisationByIdQueryHandler : IRequestHandler<GetOrganisationByIdQuery, OrganisationDto>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<GetOrganisationByIdQuery> _validator;

        public GetOrganisationByIdQueryHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<GetOrganisationByIdQuery> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task<OrganisationDto> Handle(GetOrganisationByIdQuery request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }

            if (!_currentUser.IsSuperAdmin && _currentUser.OrganisationId != request.OrganisationId)
                throw new ForbiddenException("You can only view your own organisation.");

            var org = await _db.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == request.OrganisationId && !o.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Organisation", request.OrganisationId);

            return new OrganisationDto(
                org.Id,
                org.Name,
                org.NormalizedEmail,
                org.NormalizedPhoneNumber,
                org.Type,
                org.Status,
                org.IsOperational,
                org.CreatedAtUtc,
                org.CreatedBy,
                org.LastModifiedAtUtc,
                org.LastModifiedBy);
        }
    }
}
