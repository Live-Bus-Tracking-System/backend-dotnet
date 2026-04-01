using BusTracker.Domain.Entities;
using BusTracker.Domain.Enums;
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

            builder.Property(r => r.OrganizationId)
                .IsRequired(false);

            builder.HasOne(r => r.Organization)
                .WithMany()
                .HasForeignKey(r => r.OrganizationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(r => r.IsGoverned)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(r => r.DataOrigin)
                .IsRequired()
                .HasDefaultValue(DataOrigin.Manual)
                .HasConversion<int>();

            builder.HasMany(r => r.RouteStops)
                .WithOne(rs => rs.Route)
                .HasForeignKey(rs => rs.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}