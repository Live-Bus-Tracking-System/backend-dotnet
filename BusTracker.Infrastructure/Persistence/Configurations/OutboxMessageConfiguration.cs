using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.Type)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(o => o.Content)
                .IsRequired();

            builder.HasIndex(o => new { o.ProcessedOnUtc, o.RetryCount });
        }
    }
}
