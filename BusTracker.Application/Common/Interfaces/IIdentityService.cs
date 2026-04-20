using BusTracker.Application.Common.Models;

namespace BusTracker.Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<UserAuthDto> AuthenticateAsync(string emailOrPhone, string password);

        Task<string> CreateUserAsync(string fullName, string? email, string phoneNumber, string password);

        Task<string> GeneratePasswordResetTokenAsync(string emailOrPhone);

        Task ResetPasswordAsync(string emailOrPhone, string token, string newPassword);

        Task ChangePasswordAsync(string userId, string currentPassword, string newPassword);

        Task<UserAuthDto> GetUserByIdAsync(string userId);

        Task AssignUserToOrganisationAsync(string userId, Guid organisationId, string role);

        Task RemoveUsersFromOrganisationAsync(Guid organisationId, CancellationToken cancellationToken);

        Task<bool> CheckPasswordAsync(string userId, string password);
    }
}
