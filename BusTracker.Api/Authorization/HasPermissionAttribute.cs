using Microsoft.AspNetCore.Authorization;

namespace BusTracker.Api.Authorization
{
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        private const string PolicyPrefix = "permission:";

        public HasPermissionAttribute(string permission)
        {
            Policy = $"{PolicyPrefix}{permission}";
        }
    }
}
