using BusTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusTracker.Infrastructure.Persistence.Configurations
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.Property(s => s.FullName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.AdmissionNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(s => new { s.OrganizationId, s.AdmissionNumber }).IsUnique();

            builder.HasQueryFilter(o => !o.IsDeleted);
        }
    }
}