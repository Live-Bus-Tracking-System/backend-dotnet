using BusTracker.Domain.Common;
using BusTracker.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace BusTracker.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Organization> Organizations => Set<Organization>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Route> Routes => Set<Route>();
        public DbSet<Stop> Stops => Set<Stop>();
        public DbSet<RouteStop> RouteStops => Set<RouteStop>();
        public DbSet<ActiveAssignment> ActiveAssignments => Set<ActiveAssignment>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<StudentGuardian> StudentGuardians => Set<StudentGuardian>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModifiedAtUtc = DateTime.UtcNow;
                        break;
                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.Entity.IsDeleted = true;
                        entry.Entity.DeletedAtUtc = DateTime.UtcNow;
                        // entry.Entity.DeletedBy = "System"; TODO: Inject user service to get current user here.
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}