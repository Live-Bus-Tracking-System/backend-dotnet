using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class VehicleExpectedRouteConfiguration : IEntityTypeConfiguration<VehicleExpectedRoute>
    {
        public void Configure(EntityTypeBuilder<VehicleExpectedRoute> builder)
        {
            // Prevent SQL Server "multiple cascade paths" error.
            // Both FKs default to CASCADE, but SQL Server can't have two cascade paths
            // converging on the same table when they share a common ancestor (Organization).
            // Setting both to Restrict (ON DELETE NO ACTION) resolves the cycle.

            builder.HasOne(ver => ver.Vehicle)
                .WithMany(v => v.ExpectedRoutes)
                .HasForeignKey(ver => ver.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ver => ver.Route)
                .WithMany()
                .HasForeignKey(ver => ver.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(ver => !ver.IsDeleted);
        }
    }
}
