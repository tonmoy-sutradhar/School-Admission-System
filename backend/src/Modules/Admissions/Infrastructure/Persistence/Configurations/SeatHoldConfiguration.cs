using Admissions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Admissions.Infrastructure.Persistence.Configurations;

public sealed class SeatHoldConfiguration : IEntityTypeConfiguration<SeatHold>
{
    public void Configure(EntityTypeBuilder<SeatHold> builder)
    {
        builder.ToTable("seat_holds");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClassId).IsRequired();
        builder.Property(x => x.StudentId).HasMaxLength(17).IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.ConfirmedAtUtc);

        builder.HasIndex(x => new { x.ClassId, x.Status, x.ExpiresAtUtc })
            .HasDatabaseName("ix_seat_holds_class_status_expires");

        builder.HasIndex(x => x.StudentId)
            .HasDatabaseName("ix_seat_holds_student");

        builder.HasIndex(x => new { x.StudentId, x.ClassId })
            .IsUnique()
            .HasFilter("\"Status\" IN ('HELD', 'CONFIRMED')")
            .HasDatabaseName("ux_seat_holds_active_student_class");

        builder.HasOne<AdmissionClass>()
            .WithMany()
            .HasForeignKey(x => x.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "ck_seat_holds_status",
            "\"Status\" IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')"));

        builder.Ignore(x => x.DomainEvents);
    }
}
