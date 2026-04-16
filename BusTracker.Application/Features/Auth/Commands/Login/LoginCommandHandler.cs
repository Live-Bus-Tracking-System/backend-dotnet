using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Repository;
using BusTracker.Domain.Entities;
using FluentValidation;
using MediatR;
using BusTracker.Application.Common.Interfaces.Services;
using BusTracker.Application.Features.Auth.Events;

namespace BusTracker.Application.Features.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
    {
        private readonly IIdentityService _identityService;
        private readonly IJwtTokenGenerator _jwtGenerator;
        private readonly IAuthRepository _authRepository;
        private readonly IValidator<LoginCommand> _validator;
        private readonly IEventService _eventService;
        public LoginCommandHandler(
            IIdentityService identityService,
            IJwtTokenGenerator jwtGenerator,
            IAuthRepository authRepository,
            IValidator<LoginCommand> validator,
            IEventService eventService)
        {
            _identityService = identityService;
            _jwtGenerator = jwtGenerator;
            _authRepository = authRepository;
            _validator = validator;
            _eventService = eventService;
        }

        public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }
            // 1. Authenticate user (Throws UnauthorizedException if failed)
            var user = await _identityService.AuthenticateAsync(request.EmailOrPhone, request.Password);

            // 2. Generate Tokens
            var accessToken = _jwtGenerator.GenerateAccessToken(user);
            var (rawRefreshToken, refreshTokenHash) = _jwtGenerator.GenerateRefreshToken();

            // 3. Track Session (RefreshToken in DB)
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(7),
                IpAddress = request.IpAddress,
                UserAgent = request.UserAgent
            };

            await _authRepository.SaveRefreshTokenAsync(refreshTokenEntity, cancellationToken);

            await _eventService.EmitAsync(new LoginEvent(user.Id, request.IpAddress), cancellationToken);

            return new LoginResult(user, accessToken, rawRefreshToken);
        }
    }
}
