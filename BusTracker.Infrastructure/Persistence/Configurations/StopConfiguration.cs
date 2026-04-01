using BusTracker.Domain.Entities;
using BusTracker.Domain.Enums;
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

            builder.Property(s => s.OrganizationId)
                .IsRequired(false);

            builder.HasOne(s => s.Organization)
                .WithMany()
                .HasForeignKey(s => s.OrganizationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(s => s.IsGlobal)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(s => s.DataOrigin)
                .IsRequired()
                .HasDefaultValue(DataOrigin.Manual)
                .HasConversion<int>();

            builder.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}