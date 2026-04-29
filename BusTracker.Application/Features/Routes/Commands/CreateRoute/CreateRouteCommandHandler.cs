using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Entities;
using BusTracker.Domain.Enums;
using BusTracker.Domain.Events.Routes;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Routes.Commands.CreateRoute
{
    public class CreateRouteCommandHandler : IRequestHandler<CreateRouteCommand, Guid>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<CreateRouteCommand> _validator;

        public CreateRouteCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IValidator<CreateRouteCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task<Guid> Handle(CreateRouteCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }

            if (_currentUser.OrganisationType == OrganizationType.PublicTransit.ToString() && !_currentUser.IsSuperAdmin)
            {
                throw new ForbiddenException("Public Transit organizations cannot manage routes directly. Please contact a SuperAdmin.");
            }

            Guid? orgId = _currentUser.IsSuperAdmin ? null : _currentUser.OrganisationId;
            
            // If the user provided stops, validate them
            if (request.Stops != null && request.Stops.Any())
            {
                var stopIds = request.Stops.Select(s => s.StopId).Distinct().ToList();
                var dbStops = await _db.Stops
                    .Where(s => stopIds.Contains(s.Id) && !s.IsDeleted)
                    .ToListAsync(cancellationToken);

                if (dbStops.Count != stopIds.Count)
                {
                    throw new BadRequestException("One or more provided StopIds are invalid or deleted.");
                }

                // Security check: Only use stops that are Global OR belong to your Org
                if (!_currentUser.IsSuperAdmin)
                {
                    foreach (var stop in dbStops)
                    {
                        if (!stop.IsGlobal && stop.OrganizationId != orgId)
                        {
                            throw new ForbiddenException($"Cannot add stop {stop.Id} as it belongs to a different organization.");
                        }
                    }
                }
            }

            var route = new Route
            {
                RouteNumber = request.RouteNumber,
                RouteShapeCoordinates = request.FullPolyline,
                OrganizationId = orgId,
                IsPublic = request.IsPublic,
                DataOrigin = DataOrigin.Manual
            };

            if (request.Stops != null && request.Stops.Any())
            {
                // Ensure sequences are unique and ordered correctly
                var orderedStops = request.Stops.OrderBy(s => s.Sequence).ToList();
                
                foreach (var inputStop in orderedStops)
                {
                    var routeStop = new RouteStop
                    {
                        StopId = inputStop.StopId,
                        StopSequence = inputStop.Sequence,
                        PathToNextStop = inputStop.SegmentPolyline,
                        DistanceToNextStopMeters = inputStop.Distance
                    };
                    route.RouteStops.Add(routeStop);
                }
            }

            // Emit the event so the self-healing cache logic can invalidate/update
            route.AddDomainEvent(new RouteConfigurationChangedDomainEvent(route.Id, route.RouteNumber, route.OrganizationId));

            _db.Routes.Add(route);
            await _db.SaveChangesAsync(cancellationToken);

            return route.Id;
        }
    }
}
