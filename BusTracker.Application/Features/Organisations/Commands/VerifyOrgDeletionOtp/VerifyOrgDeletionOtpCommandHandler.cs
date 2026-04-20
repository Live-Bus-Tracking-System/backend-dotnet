using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BusTracker.Application.Features.Organisations.Commands.VerifyOrgDeletionOtp
{
    public class VerifyOrgDeletionOtpCommandHandler : IRequestHandler<VerifyOrgDeletionOtpCommand, VerifyOrgDeletionOtpResult>
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IOrgDeletionIntentCache _intentCache;
        private readonly IValidator<VerifyOrgDeletionOtpCommand> _validator;

        public VerifyOrgDeletionOtpCommandHandler(
            ICurrentUserService currentUser,
            IOrgDeletionIntentCache intentCache,
            IValidator<VerifyOrgDeletionOtpCommand> validator)
        {
            _currentUser = currentUser;
            _intentCache = intentCache;
            _validator = validator;
        }

        public async Task<VerifyOrgDeletionOtpResult> Handle(VerifyOrgDeletionOtpCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }

            if (_currentUser.UserId == null)
                throw new UnauthorizedException("User not authenticated.");

            var intent = await _intentCache.GetOtpIntentAsync(request.IntentId);
            if (intent == null)
            {
                throw new BadRequestException("OTP session expired or invalid.");
            }

            if (intent.OrgId != request.OrganisationId || intent.UserId != _currentUser.UserId)
            {
                throw new ForbiddenException("Invalid request parameters.");
            }

            using var sha256 = SHA256.Create();
            var otpBytes = Encoding.UTF8.GetBytes(request.Otp + request.IntentId);
            var otpHash = Convert.ToBase64String(sha256.ComputeHash(otpBytes));

            if (intent.OtpHash != otpHash)
            {
                throw new ForbiddenException("Invalid OTP.");
            }

            await _intentCache.RemoveOtpIntentAsync(request.IntentId);

            var confirmToken = Guid.NewGuid().ToString();
            var confirmIntent = new OrgDeletionConfirmIntent(request.OrganisationId, _currentUser.UserId);
            
            await _intentCache.StoreConfirmTokenAsync(confirmToken, confirmIntent, TimeSpan.FromMinutes(15));

            return new VerifyOrgDeletionOtpResult(confirmToken);
        }
    }
}
