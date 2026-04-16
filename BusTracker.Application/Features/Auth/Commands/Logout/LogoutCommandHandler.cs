using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Repository;
using MediatR;

namespace BusTracker.Application.Features.Auth.Commands.Logout
{
    public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
    {
        private readonly IAuthRepository _authRepository;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public LogoutCommandHandler(IAuthRepository authRepository, IJwtTokenGenerator jwtTokenGenerator)
        {
            _authRepository = authRepository;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrEmpty(request.RawRefreshToken))
            {
                return;
            }

            var hash = _jwtTokenGenerator.HashRefreshToken(request.RawRefreshToken);
            var tokenEntity = await _authRepository.GetActiveRefreshTokenAsync(hash, cancellationToken);

            if (tokenEntity != null)
            {
                tokenEntity.IsRevoked = true;
                tokenEntity.RevokedAtUtc = DateTime.UtcNow;

                await _authRepository.UpdateRefreshTokenAsync(tokenEntity, cancellationToken);
            }
        }
    }
}
