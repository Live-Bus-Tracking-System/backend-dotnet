using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class RouteConfiguration : IEntityTypeConfiguration<Route>
    {
        public void Configure(EntityTypeBuilder<Route> builder)
        {
            builder.Property(r => r.RouteNumber)
                .IsRequired(false)
                .HasMaxLength(20);

            builder.HasMany(r => r.RouteStops)
                .WithOne(rs => rs.Route)
                .HasForeignKey(rs => rs.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}