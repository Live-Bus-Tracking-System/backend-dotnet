using BusTracker.Application.Common.Exceptions;
using BusTracker.Application.Common.Interfaces;
using BusTracker.Application.Common.Models;
using BusTracker.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Infrastructure.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IPhoneNumberService _phoneNumberService;

        public IdentityService(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IPhoneNumberService phoneNumberService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _phoneNumberService = phoneNumberService;
        }

        public async Task<UserAuthDto> AuthenticateAsync(string emailOrPhone, string password)
        {
            // Support login by Email OR Phone
            bool isEmail = new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(emailOrPhone);

            ApplicationUser? user;

            if (isEmail)
            {
                user = await _userManager.FindByEmailAsync(emailOrPhone);
            }
            else
            {
                var normalizedPhone = _phoneNumberService.Normalize(emailOrPhone);
                if (normalizedPhone == null) throw new UnauthorizedException("Invalid credentials.");

                user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            }

            if (user == null)
                throw new UnauthorizedException("Invalid credentials.");

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                if (result.IsLockedOut) throw new UnauthorizedException("Account locked."); // TODO: NOTIFY THE user about the lockout via email or phone
                throw new UnauthorizedException("Invalid credentials.");
            }

            return await BuildAuthDto(user);
        }

        public async Task<string> CreateUserAsync(string fullName, string? email, string phoneNumber, string password)
        {
            var normalizedPhone = _phoneNumberService.Normalize(phoneNumber);
            if (normalizedPhone == null) 
                throw new CustomValidationException(new[] { new FluentValidation.Results.ValidationFailure("PhoneNumber", "Invalid phone number format.") });

            // Double check if phone or email already exists
            if (await _userManager.Users.AnyAsync(u => u.PhoneNumber == normalizedPhone))
                throw new CustomValidationException(new[] { new FluentValidation.Results.ValidationFailure("PhoneNumber", "Phone number is already in use.") });

            if (!string.IsNullOrEmpty(email) && await _userManager.FindByEmailAsync(email) != null)
                throw new CustomValidationException(new[] { new FluentValidation.Results.ValidationFailure("Email", "Email is already in use.") });

            var user = new ApplicationUser
            {
                UserName = email ?? normalizedPhone,
                Email = email,
                PhoneNumber = normalizedPhone,
                FullName = fullName
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new FluentValidation.Results.ValidationFailure(e.Code, e.Description));
                throw new CustomValidationException(errors);
            }

            await _userManager.AddToRoleAsync(user, "Passenger");

            return user.Id;
        }

        public async Task ChangePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("User", userId);

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new FluentValidation.Results.ValidationFailure(e.Code, e.Description));
                throw new CustomValidationException(errors);
            }

            // Immediately invalidate all tokens via SecurityStamp bump
            await _userManager.UpdateSecurityStampAsync(user);
        }

        public async Task<string> GeneratePasswordResetTokenAsync(string emailOrPhone)
        {
            bool isEmail = new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(emailOrPhone);

            ApplicationUser? user;

            if (isEmail)
            {
                user = await _userManager.FindByEmailAsync(emailOrPhone);
            }
            else
            {
                var normalizedPhone = _phoneNumberService.Normalize(emailOrPhone);
                if (normalizedPhone == null) throw new UnauthorizedException("Invalid credentials.");

                user = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            }

            if (user == null) throw new NotFoundException("User not found.");

            return await _userManager.GeneratePasswordResetTokenAsync(user);
        }

        public async Task ResetPasswordAsync(string emailOrPhone, string token, string newPassword)
        {
            var user = await _userManager.FindByEmailAsync(emailOrPhone);
            if (user == null)
            {
                var normalizedPhone = _phoneNumberService.Normalize(emailOrPhone);
                if (normalizedPhone != null)
                {
                    user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
                }
            }

            if (user == null) throw new NotFoundException("User not found.");

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => new FluentValidation.Results.ValidationFailure(e.Code, e.Description));
                throw new CustomValidationException(errors);
            }

            await _userManager.UpdateSecurityStampAsync(user);
        }

        public async Task<UserAuthDto> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) throw new NotFoundException("User", userId);

            return await BuildAuthDto(user);
        }

        private async Task<UserAuthDto> BuildAuthDto(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            return new UserAuthDto
            {
                Id = user.Id,
                Email = user.Email ?? "",
                Phone = user.PhoneNumber ?? "",
                FullName = user.FullName,
                Roles = roles,
                SecurityStamp = user.SecurityStamp,
                OrganizationId = user.OrganizationId?.ToString(),
                // If organization exists, read the Type (requires EF inclusion, simplified here)
                OrganizationType = user.OrganizationId != null ? "PublicTransit" : null // Expand logic when Org loaded.
            };
        }
    }
}