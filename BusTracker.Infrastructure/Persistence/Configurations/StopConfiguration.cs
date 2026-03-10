using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class StopConfiguration : IEntityTypeConfiguration<Stop>
    {
        public void Configure(EntityTypeBuilder<Stop> builder)
        {
            builder.Property(s => s.StopName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.Location)
                .IsRequired();

            builder.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}