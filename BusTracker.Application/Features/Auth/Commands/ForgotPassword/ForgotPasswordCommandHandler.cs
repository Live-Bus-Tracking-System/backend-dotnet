using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Application.Features.Auth.Events;

namespace BusTracker.Application.Features.Auth.Commands.ForgotPassword
{
    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, string>
    {
        private readonly IIdentityService _identityService;
        private readonly IValidator<ForgotPasswordCommand> _validator;
        private readonly IEventService _eventService;
        public ForgotPasswordCommandHandler(IIdentityService identityService, IValidator<ForgotPasswordCommand> validator, IEventService eventService)
        {
            _identityService = identityService;
            _validator = validator;
            _eventService = eventService;
        }

        public async Task<string> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }
            var token = await _identityService.GeneratePasswordResetTokenAsync(request.EmailOrPhone);

            await _eventService.EmitAsync(new ForgotPasswordEvent(request.EmailOrPhone, token), cancellationToken);

            // In MVP production: Returning token directly. 
            // In complete production: This handler fires an Event which the Email sender picks up, and this handler returns Unit.
            return token;
        }
    }
}
