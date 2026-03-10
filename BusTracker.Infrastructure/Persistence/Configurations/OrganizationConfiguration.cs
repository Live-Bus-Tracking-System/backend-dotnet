using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.NormalizedEmail)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.NormalizedPhoneNumber)
                .IsRequired()
                .HasMaxLength(15);

            builder.HasIndex(o => o.NormalizedEmail).IsUnique();
            builder.HasIndex(o => o.NormalizedPhoneNumber).IsUnique();

            builder.Property(o => o.HashedInviteCode)
                .HasMaxLength(512);

            builder.HasIndex(o => o.HashedInviteCode)
                .IsUnique()
                .HasFilter("[HashedInviteCode] IS NOT NULL");

            builder.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}