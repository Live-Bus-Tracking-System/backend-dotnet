using BusTracker.Domain.Entities;
using BusTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class ComplianceDocumentConfiguration : IEntityTypeConfiguration<ComplianceDocument>
    {
        public void Configure(EntityTypeBuilder<ComplianceDocument> builder)
        {
            builder.HasIndex(cd => new { cd.EntityType, cd.EntityId });

            builder.HasIndex(cd => new { cd.EntityType, cd.EntityId, cd.DocumentType });

            builder.Property(cd => cd.EntityType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(cd => cd.DocumentType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(cd => cd.DocumentUrl)
                .IsRequired()
                .HasMaxLength(2048);

            builder.Property(cd => cd.DocumentNumber)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(cd => cd.IssuedBy)
                .IsRequired(false)
                .HasMaxLength(200);

            builder.Property(cd => cd.VerifiedBy)
                .IsRequired(false)
                .HasMaxLength(450);

            builder.Property(cd => cd.IsVerified)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(cd => cd.Notes)
                .IsRequired(false)
                .HasMaxLength(1000);

            builder.HasQueryFilter(cd => !cd.IsDeleted);
        }
    }
}
