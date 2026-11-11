using Admissions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Admissions.Infrastructure.Persistence.Configurations;

public sealed class AdmissionClassConfiguration : IEntityTypeConfiguration<AdmissionClass>
{
    public void Configure(EntityTypeBuilder<AdmissionClass> builder)
    {
        builder.ToTable("admission_classes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TotalSeats).IsRequired();
        builder.ToTable(t => t.HasCheckConstraint("ck_admission_classes_total_seats", "\"TotalSeats\" > 0"));
        builder.Ignore(x => x.DomainEvents);
    }
}
