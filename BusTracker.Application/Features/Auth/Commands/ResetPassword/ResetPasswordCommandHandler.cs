using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Application.Features.Auth.Events;

namespace BusTracker.Application.Features.Auth.Commands.ResetPassword
{
    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
    {
        private readonly IIdentityService _identityService;
        private readonly IValidator<ResetPasswordCommand> _validator;
        private readonly IEventService _eventService;
        public ResetPasswordCommandHandler(IIdentityService identityService, IValidator<ResetPasswordCommand> validator, IEventService eventService)
        {
            _identityService = identityService;
            _validator = validator;
            _eventService = eventService;
        }

        public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }
            await _identityService.ResetPasswordAsync(request.EmailOrPhone, request.Token, request.NewPassword);

            await _eventService.EmitAsync(new ResetPasswordEvent(request.EmailOrPhone), cancellationToken);
        }
    }
}
