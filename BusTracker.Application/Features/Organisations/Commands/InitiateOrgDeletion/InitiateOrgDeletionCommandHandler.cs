using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BusTracker.Application.Features.Organisations.Commands.InitiateOrgDeletion
{
    public class InitiateOrgDeletionCommandHandler : IRequestHandler<InitiateOrgDeletionCommand, InitiateOrgDeletionResult>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUserService _currentUser;
        private readonly IIdentityService _identityService;
        private readonly IOrgDeletionIntentCache _intentCache;
        private readonly IEmailService _emailService;
        private readonly ITemplateService _templateService;
        private readonly IValidator<InitiateOrgDeletionCommand> _validator;

        public InitiateOrgDeletionCommandHandler(
            IApplicationDbContext db,
            ICurrentUserService currentUser,
            IIdentityService identityService,
            IOrgDeletionIntentCache intentCache,
            IEmailService emailService,
            ITemplateService templateService,
            IValidator<InitiateOrgDeletionCommand> validator)
        {
            _db = db;
            _currentUser = currentUser;
            _identityService = identityService;
            _intentCache = intentCache;
            _emailService = emailService;
            _templateService = templateService;
            _validator = validator;
        }

        public async Task<InitiateOrgDeletionResult> Handle(InitiateOrgDeletionCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new CustomValidationException(validationResult.Errors);
            }

            if (_currentUser.UserId == null)
                throw new UnauthorizedException("User not authenticated.");

            if (_currentUser.OrganisationId != request.OrganisationId)
                throw new ForbiddenException("You do not have permission to delete this organisation.");

            var org = await _db.Organizations
                .FirstOrDefaultAsync(o => o.Id == request.OrganisationId && !o.IsDeleted, cancellationToken)
                ?? throw new NotFoundException("Organisation", request.OrganisationId);

            var isPasswordValid = await _identityService.CheckPasswordAsync(_currentUser.UserId, request.Password);
            if (!isPasswordValid)
            {
                throw new UnauthorizedException("Invalid password.");
            }

            var otpCode = new Random().Next(100000, 999999).ToString();
            var intentId = Guid.NewGuid().ToString();

            using var sha256 = SHA256.Create();
            var otpBytes = Encoding.UTF8.GetBytes(otpCode + intentId);
            var otpHash = Convert.ToBase64String(sha256.ComputeHash(otpBytes));

            var intent = new OrgDeletionOtpIntent(request.OrganisationId, _currentUser.UserId, otpHash);
            await _intentCache.StoreOtpIntentAsync(intentId, intent, TimeSpan.FromMinutes(10));

            var emailModel = new
            {
                OrgName = org.Name,
                Otp = otpCode
            };

            var htmlBody = await _templateService.RenderTemplateAsync("OrgDeletionOtpEmail.html", emailModel);
            
            await _emailService.SendEmailAsync(
                to: org.NormalizedEmail,
                subject: "BusTracker - Organisation Deletion Verification Code",
                htmlBody: htmlBody,
                cancellationToken: cancellationToken);

            return new InitiateOrgDeletionResult(intentId);
        }
    }
}
