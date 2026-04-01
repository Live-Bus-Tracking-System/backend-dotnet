using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

// This attribute tells the User Secrets system which secret store to load.
// It must match the <UserSecretsId> in BusTracker.Api.csproj.
[assembly: Microsoft.Extensions.Configuration.UserSecrets.UserSecretsIdAttribute("0dd3ff35-bac8-430e-98e9-7ae6c7ddf9fa")]

namespace BusTracker.Infrastructure.Persistence
{
    /// <summary>
    /// Used EXCLUSIVELY by EF Core CLI/PM tools at design time (migrations, drop-database, etc.).
    /// It bypasses Program.cs entirely so Redis, SignalR and other runtime services
    /// never get invoked during EF tooling operations.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Walk up from the Infrastructure project to find the API appsettings.json
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "BusTracker.Api");

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                // Loads the User Secrets store registered via [assembly: UserSecretsId] above
                .AddUserSecrets<ApplicationDbContextFactory>(optional: true)
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection string not found. Make sure it is in appsettings.json or User Secrets.");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connectionString, builder =>
            {
                builder.UseNetTopologySuite();
            });

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}
