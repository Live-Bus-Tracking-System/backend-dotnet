namespace BusTracker.Application.Common.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }

        string? Email { get; }

        //string? PhoneNumber { get; }

        string? IpAddress { get; }

        bool IsAuthenticated { get; }

        bool IsSuperAdmin { get; }

        Guid? OrganisationId { get; }

        string? OrganisationType { get; }
    }
}
