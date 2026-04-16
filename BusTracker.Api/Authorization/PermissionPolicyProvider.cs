using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BusTracker.Api.Authorization
{
    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        private const string PolicyPrefix = "permission:";

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
            : base(options)
        {
        }

        public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // First check if it's a default policy (e.g. Roles)
            var policy = await base.GetPolicyAsync(policyName);

            if (policy == null && policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var permission = policyName.Substring(PolicyPrefix.Length);

                var policyBuilder = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(permission));

                return policyBuilder.Build();
            }

            return policy;
        }
    }
}
