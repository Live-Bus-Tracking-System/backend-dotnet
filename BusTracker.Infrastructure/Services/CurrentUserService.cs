using BusTracker.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BusTracker.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public string? UserId =>
            User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? Email =>
            User?.FindFirstValue(ClaimTypes.Email)
            ?? User?.FindFirstValue("email");

        public string? IpAddress =>
            _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        public bool IsAuthenticated =>
            User?.Identity?.IsAuthenticated == true;

        public bool IsSuperAdmin =>
            User?.IsInRole("SuperAdmin") == true;

        public Guid? OrganisationId
        {
            get
            {
                var raw = User?.FindFirstValue("orgId");
                return Guid.TryParse(raw, out var id) ? id : null;
            }
        }

        public string? OrganisationType =>
            User?.FindFirstValue("orgType");
    }
}
