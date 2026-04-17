using BusTracker.Application.Common.Auth;
using BusTracker.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Index.Quadtree;
using System.Security.Claims;


namespace BusTracker.Infrastructure.Seeders
{
    public static class SuperAdminSeeder
    {
        private sealed class Log { }

        // Role → permission list mapping
        private static readonly Dictionary<string, IReadOnlyList<string>> RolePermissions = new()
        {
            [Roles.SuperAdmin] = Permissions.SuperAdminPermissions,
            [Roles.TransitAuthorityAdmin] = Permissions.TransitAuthorityAdminPermissions,
            [Roles.OrgAdmin] = Permissions.OrgAdminPermissions,
            [Roles.OrgStaff] = Permissions.OrgStaffPermissions,
            [Roles.Passenger] = Permissions.PassengerPermissions,
        };

        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var config = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILogger<Log>>();

            await SeedRolesAndPermissionsAsync(roleManager, logger);
            await SeedSuperAdminUserAsync(userManager, config, logger);
        }

        // ── Step 1: Roles + RoleClaims ───────────────────────────────────────────

        private static async Task SeedRolesAndPermissionsAsync(
            RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            foreach (var (roleName, permissions) in RolePermissions)
            {
                // Create role if missing
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!result.Succeeded)
                    {
                        logger.LogError("Failed to create role {Role}: {Errors}",
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                        continue;
                    }
                    logger.LogInformation("Created role: {Role}", roleName);
                }

                // Sync permissions into AspNetRoleClaims
                var role = await roleManager.FindByNameAsync(roleName);
                var existingClaims = await roleManager.GetClaimsAsync(role!);
                var existingPerms = existingClaims
                    .Where(c => c.Type == "permission")
                    .Select(c => c.Value)
                    .ToHashSet();

                // 1. Add missing permissions
                foreach (var permission in permissions)
                {
                    if (!existingPerms.Contains(permission))
                    {
                        var result = await roleManager.AddClaimAsync(role!, new Claim("permission", permission));
                        if (!result.Succeeded)
                        {
                            logger.LogWarning("Failed to add permission {Permission} to role {Role}: {Errors}",
                                permission, roleName,
                                string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                    }
                }

                // 2. Remove stale permissions that are no longer in code
                var expectedPerms = permissions.ToHashSet();
                foreach (var existingClaim in existingClaims.Where(c => c.Type == "permission"))
                {
                    if (!expectedPerms.Contains(existingClaim.Value))
                    {
                        var result = await roleManager.RemoveClaimAsync(role!, existingClaim);
                        if (!result.Succeeded)
                        {
                            logger.LogWarning("Failed to remove stale permission {Permission} from role {Role}: {Errors}",
                                existingClaim.Value, roleName,
                                string.Join(", ", result.Errors.Select(e => e.Description)));
                        }
                        else
                        {
                            logger.LogInformation("Removed stale permission {Permission} from role {Role}", existingClaim.Value, roleName);
                        }
                    }
                }

                logger.LogInformation("Permissions synced for role: {Role}", roleName);
            }
        }

        // ── Step 2: SuperAdmin user ───────────────────────────────────────────────

        private static async Task SeedSuperAdminUserAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            ILogger logger)
        {
            var email = config["SuperAdmin:Email"];
            var password = config["SuperAdmin:Password"];

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning(
                    "SuperAdmin credentials not configured. " +
                    "Set 'SuperAdmin:Email' and 'SuperAdmin:Password' in User Secrets.");
                return;
            }

            // Already exists — nothing to do
            if (await userManager.FindByEmailAsync(email) is not null)
            {
                logger.LogInformation("SuperAdmin user already exists, skipping seed.");
                return;
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create SuperAdmin user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            var roleResult = await userManager.AddToRoleAsync(user, Roles.SuperAdmin);
            if (!roleResult.Succeeded)
            {
                logger.LogError("Failed to assign SuperAdmin role: {Errors}",
                    string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("SuperAdmin user seeded successfully: {Email}", email);
        }
    }
}
