using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusTracker.Application.Common.Interfaces
{
    /// <summary>
    /// Application layer abstraction over the database context.
    /// Handlers depend on this interface, never on the concrete EF DbContext directly.
    /// </summary>
    public interface IApplicationDbContext
    {
        DbSet<Organization> Organizations { get; }
        DbSet<Vehicle> Vehicles { get; }
        DbSet<VehiclePermit> VehiclePermits { get; }
        DbSet<Route> Routes { get; }
        DbSet<Stop> Stops { get; }
        DbSet<RouteStop> RouteStops { get; }
        DbSet<ComplianceDocument> ComplianceDocuments { get; }
        DbSet<ActiveAssignment> ActiveAssignments { get; }
        DbSet<Student> Students { get; }
        DbSet<OutboxMessage> OutboxMessages { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
