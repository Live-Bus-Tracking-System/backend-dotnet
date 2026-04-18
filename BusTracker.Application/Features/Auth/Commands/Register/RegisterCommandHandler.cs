using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Repository;
using BusTracker.Application.Features.Auth.Commands.Login;
using BusTracker.Domain.Entities;
using FluentValidation;
using MediatR;
using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Application.Features.Auth.Events;

namespace BusTracker.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, LoginResult>
    {
        private readonly IIdentityService _identityService;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IAuthRepository _authRepository;
        private readonly IValidator<RegisterCommand> _validator;
        private readonly IEventService _eventService;

        public RegisterCommandHandler(
            IIdentityService identityService,
            IJwtTokenGenerator jwtGenerator,
            IAuthRepository authRepository,
            IValidator<RegisterCommand> validator,
            IEventService eventService)
        {
            _identityService = identityService;
            _jwtGenerator = jwtGenerator;
            _authRepository = authRepository;
            _validator = validator;
            _eventService = eventService;
        }

        public async Task<LoginResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }
            var userId = await _identityService.CreateUserAsync(
                request.FullName,
                request.Email,
                request.PhoneNumber,
                request.Password);

            await _eventService.EmitAsync(new RegisterEvent(userId, request.FullName, request.Email, request.PhoneNumber), cancellationToken);

            var user = await _identityService.GetUserByIdAsync(userId);

            var accessToken = _jwtGenerator.GenerateAccessToken(user);
            var (rawRefreshToken, refreshTokenHash) = _jwtGenerator.GenerateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                UserId        = user.Id,
                TokenHash     = refreshTokenHash,
                ExpiresAtUtc  = DateTime.UtcNow.AddDays(7),
                IpAddress     = request.IpAddress,
                UserAgent     = request.UserAgent,
                SecurityStamp = user.SecurityStamp
            };

            await _authRepository.SaveRefreshTokenAsync(refreshTokenEntity, cancellationToken);

            return new LoginResult(user, accessToken, rawRefreshToken);
        }
    }
}
