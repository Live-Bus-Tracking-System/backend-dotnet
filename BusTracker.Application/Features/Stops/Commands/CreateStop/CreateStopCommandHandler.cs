using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Domain.Entities;
using BusTracker.Domain.Enums;
using FluentValidation;
using MediatR;
using NetTopologySuite.Geometries;

namespace BusTracker.Application.Features.Stops.Commands.CreateStop
{
    public class CreateStopCommandHandler : IRequestHandler<CreateStopCommand, Guid>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<CreateStopCommand> _validator;

        public CreateStopCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser, IValidator<CreateStopCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _validator = validator;
        }

        public async Task<Guid> Handle(CreateStopCommand request, CancellationToken cancellationToken)
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

            if (request.IsGlobal && !_currentUser.IsSuperAdmin)
            {
                throw new ForbiddenException("Only SuperAdmin can create global stops.");
            }

            Guid? orgId = request.IsGlobal ? null : _currentUser.OrganisationId;

            var newStop = new Stop
            {
                StopName = request.StopName,
                Location = new Point(request.Longitude, request.Latitude) { SRID = 4326 },
                IsGlobal = request.IsGlobal,
                OrganizationId = orgId,
                DataOrigin = DataOrigin.Manual
            };

            _db.Stops.Add(newStop);
            await _db.SaveChangesAsync(cancellationToken);

            return newStop.Id;
        }
    }
}
