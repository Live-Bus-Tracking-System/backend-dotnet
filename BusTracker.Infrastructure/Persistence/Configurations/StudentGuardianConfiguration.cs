using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class StudentGuardianConfiguration : IEntityTypeConfiguration<StudentGuardian>
    {
        public void Configure(EntityTypeBuilder<StudentGuardian> builder)
        {
            builder.HasOne(sg => sg.Student)
                .WithMany(s => s.Guardians)
                .HasForeignKey(sg => sg.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(builder => builder.GuardianName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(builder => builder.NormalizedPhoneNumber)
                .IsRequired()
                .HasMaxLength(15);

            builder.Property(builder => builder.NormalizedEmail)
                .IsRequired(false)
                .HasMaxLength(100);

            builder.Property(builder => builder.Relationship)
                .IsRequired(false)
                .HasMaxLength(50);

             builder.Property(builder => builder.SystemNotes)
                .IsRequired(false)
                .HasMaxLength(500);

            builder.HasIndex(sg => new { sg.StudentId, sg.NormalizedPhoneNumber })
                .IsUnique()
                .HasFilter("[NormalizedPhoneNumber] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasIndex(sg => new { sg.StudentId, sg.NormalizedEmail })
                .IsUnique()
                .HasFilter("[NormalizedEmail] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasIndex(sg => new { sg.StudentId, sg.UserId })
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL AND [IsDeleted] = 0");

            builder.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}