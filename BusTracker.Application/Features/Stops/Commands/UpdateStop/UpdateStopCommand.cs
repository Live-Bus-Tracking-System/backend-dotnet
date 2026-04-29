using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BusTracker.Application.Features.Stops.Commands.UpdateStop
{
    public record UpdateStopCommand(Guid Id, string StopName, double Latitude, double Longitude, bool IsGlobal) : IRequest;

    public class UpdateStopCommandHandler : IRequestHandler<UpdateStopCommand>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<UpdateStopCommand> _validator;

        public UpdateStopCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IValidator<UpdateStopCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task Handle(UpdateStopCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }

            if (_currentUser.OrganisationType == OrganizationType.PublicTransit.ToString() && !_currentUser.IsSuperAdmin)
            {
                throw new ForbiddenException("Public Transit organizations cannot manage stops directly. Please contact a SuperAdmin.");
            }

            var stop = await _db.Stops
                .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Stop", request.Id);

            // Authorization
            if (!_currentUser.IsSuperAdmin)
            {
                if (stop.IsGlobal || request.IsGlobal)
                {
                    throw new ForbiddenException("Only SuperAdmin can manage global stops.");
                }

                if (stop.OrganizationId != _currentUser.OrganisationId)
                {
                    throw new ForbiddenException("You cannot update a stop belonging to another organization.");
                }
            }

            stop.StopName = request.StopName;
            stop.Location = new Point(request.Longitude, request.Latitude) { SRID = 4326 };
            
            if (_currentUser.IsSuperAdmin)
            {
                stop.IsGlobal = request.IsGlobal;
                if (request.IsGlobal)
                {
                    stop.OrganizationId = null; // Global stops shouldn't belong to a specific org
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public class UpdateStopCommandValidator : AbstractValidator<UpdateStopCommand>
    {
        public UpdateStopCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.StopName).NotEmpty().MaximumLength(150);
            RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
            RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        }
    }
}
