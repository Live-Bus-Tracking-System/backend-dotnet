using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Application.Features.Auth.Events;

namespace BusTracker.Application.Features.Auth.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
    {
        private readonly IIdentityService _identityService;

        private readonly IValidator<ChangePasswordCommand> _validator;

        private readonly IEventService _eventService;

        public ChangePasswordCommandHandler(IIdentityService identityService, IValidator<ChangePasswordCommand> validator, IEventService eventService)
        {
            _identityService = identityService;
            _validator = validator;
            _eventService = eventService;
        }

        public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }

            await _identityService.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword);

            await _eventService.EmitAsync(new ChangePasswordEvent(request.UserId), cancellationToken);
        }
    }
}
