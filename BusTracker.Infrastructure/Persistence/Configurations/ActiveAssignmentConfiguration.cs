using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class ActiveAssignmentConfiguration : IEntityTypeConfiguration<ActiveAssignment>
    {
        public void Configure(EntityTypeBuilder<ActiveAssignment> builder)
        {
            builder.HasOne(a => a.Vehicle)
                .WithMany(v => v.AssignmentHistory)
                .HasForeignKey(a => a.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Route)
                .WithMany()
                .HasForeignKey(a => a.RouteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}