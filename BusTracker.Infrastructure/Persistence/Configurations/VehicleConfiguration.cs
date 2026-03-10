using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.Property(v => v.TrackerId)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(v => v.LicensePlate)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(v => v.Name)
                .HasMaxLength(50)
                .IsRequired(false);

            builder.HasIndex(v => v.TrackerId).IsUnique();

            builder.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}