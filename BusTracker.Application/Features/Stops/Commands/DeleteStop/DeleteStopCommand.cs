using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Features.Stops.Commands.DeleteStop
{
    public record DeleteStopCommand(Guid Id) : IRequest;

    public class DeleteStopCommandHandler : IRequestHandler<DeleteStopCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;

        public DeleteStopCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task Handle(DeleteStopCommand request, CancellationToken cancellationToken)
        {
            if (_currentUser.OrganisationType == OrganizationType.PublicTransit.ToString() && !_currentUser.IsSuperAdmin)
            {
                throw new ForbiddenException("Public Transit organizations cannot manage stops directly. Please contact a SuperAdmin.");
            }

            var stop = await _db.Stops
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Stop", request.Id);

            if (!_currentUser.IsSuperAdmin)
            {
                if (stop.IsGlobal)
                {
                    throw new ForbiddenException("Only SuperAdmin can delete global stops.");
                }

                if (stop.OrganizationId != _currentUser.OrganisationId)
                {
                    throw new ForbiddenException("You cannot delete a stop belonging to another organization.");
                }
            }

            // Check if stop is used in any routes. If so, prevent deletion.
            var isUsedInRoute = await _db.RouteStops.AnyAsync(rs => rs.StopId == request.Id, cancellationToken);
            if (isUsedInRoute)
            {
                throw new BadRequestException("Cannot delete this stop because it is currently assigned to one or more routes.");
            }

            _db.Stops.Remove(stop);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
