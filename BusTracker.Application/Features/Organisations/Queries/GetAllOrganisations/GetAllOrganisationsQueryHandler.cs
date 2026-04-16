using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Models;
using BusTracker.Application.Features.Organisations.DTOs;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Organisations.Queries.GetAllOrganisations
{
    public class GetAllOrganisationsQueryHandler
        : IRequestHandler<GetAllOrganisationsQuery, PagedResult<OrganisationSummaryDto>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<GetAllOrganisationsQuery> _validator;

        public GetAllOrganisationsQueryHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IValidator<GetAllOrganisationsQuery> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task<PagedResult<OrganisationSummaryDto>> Handle(GetAllOrganisationsQuery request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }

            if (!_currentUser.IsSuperAdmin)
                throw new ForbiddenException("Only SuperAdmin can list all organisations.");

            var query = _db.Organizations
                .AsNoTracking()
                .Where(o => !o.IsDeleted);

            if (request.Status.HasValue)
                query = query.Where(o => o.Status == request.Status.Value);

            if (request.Type.HasValue)
                query = query.Where(o => o.Type == request.Type.Value);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(o => o.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(o => new OrganisationSummaryDto(
                    o.Id,
                    o.Name,
                    o.NormalizedEmail,
                    o.NormalizedPhoneNumber,
                    o.Type,
                    o.Status,
                    o.CreatedAtUtc))
                .ToListAsync(cancellationToken);

            return PagedResult<OrganisationSummaryDto>.Create(items, totalCount, request.Page, request.PageSize);
        }
    }
}
