using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class RouteStopConfiguration : IEntityTypeConfiguration<RouteStop>
    {
        public void Configure(EntityTypeBuilder<RouteStop> builder)
        {

            builder.HasIndex(rs => new { rs.RouteId, rs.StopId, rs.StopSequence }).IsUnique();

            builder.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}