using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Entities;
using BusTracker.Domain.Enums;
using BusTracker.Domain.Events.Routes;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BusTracker.Application.Features.Routes.Commands.UpdateRoute
{
    public class UpdateRouteCommandHandler : IRequestHandler<UpdateRouteCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<UpdateRouteCommand> _validator;

        public UpdateRouteCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IValidator<UpdateRouteCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task Handle(UpdateRouteCommand request, CancellationToken cancellationToken)
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

            var route = await _db.Routes
                .Include(r => r.RouteStops)
                .FirstOrDefaultAsync(r => r.Id == request.RouteId && !r.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Route", request.RouteId);

            if (!_currentUser.IsSuperAdmin && route.OrganizationId != _currentUser.OrganisationId)
            {
                throw new ForbiddenException("You cannot update a route belonging to another organization.");
            }

            Guid? orgId = _currentUser.IsSuperAdmin ? null : _currentUser.OrganisationId;

            // Update primitive fields
            route.RouteNumber = request.RouteNumber;
            route.RouteShapeCoordinates = request.FullPolyline;
            route.IsPublic = request.IsPublic;

            // Clear old route stops
            route.RouteStops.Clear();

            // Rebuild route stops with inline creation / deduplication
            if (request.Stops != null && request.Stops.Any())
            {
                var orderedStops = request.Stops.OrderBy(s => s.Sequence).ToList();
                
                foreach (var inputStop in orderedStops)
                {
                    Guid resolvedStopId;

                    if (inputStop.StopId.HasValue)
                    {
                        // Use existing stop
                        var existingStop = await _db.Stops.FirstOrDefaultAsync(s => s.Id == inputStop.StopId.Value && !s.IsDeleted, cancellationToken);
                        if (existingStop == null) throw new BadRequestException($"Stop with ID {inputStop.StopId} not found.");
                        
                        if (!_currentUser.IsSuperAdmin && !existingStop.IsGlobal && existingStop.OrganizationId != orgId)
                            throw new ForbiddenException($"Cannot add stop {existingStop.Id} as it belongs to a different organization.");

                        resolvedStopId = existingStop.Id;
                    }
                    else
                    {
                        // Deduplication: Check if a stop exists within 20 meters
                        var targetPoint = new Point(inputStop.Longitude, inputStop.Latitude) { SRID = 4326 };
                        var nearbyStop = await _db.Stops
                            .Where(s => !s.IsDeleted && s.Location.Distance(targetPoint) <= 20)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (nearbyStop != null)
                        {
                            resolvedStopId = nearbyStop.Id;
                        }
                        else
                        {
                            // Create new inline stop
                            var newStop = new Stop
                            {
                                StopName = inputStop.StopName,
                                Location = targetPoint,
                                IsGlobal = true, // Inline stops managed by SuperAdmin are global to avoid duplication across routes
                                OrganizationId = null,
                                DataOrigin = DataOrigin.Manual
                            };
                            _db.Stops.Add(newStop);
                            resolvedStopId = newStop.Id;
                        }
                    }

                    var routeStop = new RouteStop
                    {
                        StopId = resolvedStopId,
                        StopSequence = inputStop.Sequence,
                        PathToNextStop = inputStop.SegmentPolyline,
                        DistanceToNextStopMeters = inputStop.Distance
                    };
                    route.RouteStops.Add(routeStop);
                }
            }

            // Emit the event so the self-healing cache logic can invalidate/update
            route.AddDomainEvent(new RouteConfigurationChangedDomainEvent(route.Id, route.RouteNumber, route.OrganizationId));

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
