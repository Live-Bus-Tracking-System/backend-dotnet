using BusTracker.Domain.Entities;
using BusTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class VehiclePermitConfiguration : IEntityTypeConfiguration<VehiclePermit>
    {
        public void Configure(EntityTypeBuilder<VehiclePermit> builder)
        {
            builder.HasIndex(vp => new { vp.VehicleId, vp.RouteId })
                .IsUnique();

            builder.HasIndex(vp => vp.OrganizationId);

            builder.HasIndex(vp => vp.PermitStatus);

            builder.HasOne(vp => vp.Vehicle)
                .WithMany()
                .HasForeignKey(vp => vp.VehicleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(vp => vp.Route)
                .WithMany()
                .HasForeignKey(vp => vp.RouteId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(vp => vp.Organization)
                .WithMany()
                .HasForeignKey(vp => vp.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(vp => vp.PermitStatus)
                .IsRequired()
                .HasDefaultValue(PermitStatus.Pending)
                .HasConversion<int>();

            builder.Property(vp => vp.PermitNumber)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(vp => vp.ApprovedBy)
                .IsRequired(false)
                .HasMaxLength(450);

            builder.Property(vp => vp.Notes)
                .IsRequired(false)
                .HasMaxLength(1000);

            builder.HasQueryFilter(vp => !vp.IsDeleted);
        }
    }
}
