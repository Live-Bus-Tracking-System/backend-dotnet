using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasIndex(rt => rt.TokenHash)
                .IsUnique();

            builder.HasIndex(rt => new { rt.UserId, rt.IsRevoked });

            builder.Property(rt => rt.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(rt => rt.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            builder.Property(rt => rt.ReplacedByTokenHash)
                .IsRequired(false)
                .HasMaxLength(64);

            builder.Property(rt => rt.IpAddress)
                .IsRequired(false)
                .HasMaxLength(45);

            builder.Property(rt => rt.UserAgent)
                .IsRequired(false)
                .HasMaxLength(255);

            builder.HasIndex(rt => rt.UserId);
        }
    }
}