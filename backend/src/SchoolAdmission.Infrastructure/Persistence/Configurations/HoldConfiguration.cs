using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Domain;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="Hold"/> to the <c>holds</c> table (ARCHITECTURE §3, ADR-0001).</summary>
public sealed class HoldConfiguration : IEntityTypeConfiguration<Hold>
{
    public void Configure(EntityTypeBuilder<Hold> builder)
    {
        builder.ToTable("holds", table =>
        {
            table.HasCheckConstraint("ck_holds_student_id_digits", "student_id ~ '^[0-9]{17}$'");
            table.HasCheckConstraint("ck_holds_status", "status IN ('HELD','CONFIRMED','EXPIRED','CANCELLED')");
        });

        builder.HasKey(h => h.Id).HasName("pk_holds");
        builder.Property(h => h.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(h => h.ClassId).HasColumnName("class_id").IsRequired();
        builder.Property(h => h.StudentId).HasColumnName("student_id").HasColumnType("varchar(17)").IsRequired();

        // Stored as the uppercase API value ('HELD', …) so raw SQL and the CHECK constraint read naturally.
        builder.Property(h => h.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .HasConversion(s => HoldStatusText.ToText(s), s => HoldStatusText.FromText(s))
            .IsRequired();

        builder.Property(h => h.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").ValueGeneratedOnAdd();
        builder.Property(h => h.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(h => h.ConfirmedAt).HasColumnName("confirmed_at");
        builder.Property(h => h.CancelledAt).HasColumnName("cancelled_at");

        // Plain REFERENCES classes(id): classes are never deleted, so no cascade.
        builder.HasOne<SchoolClass>().WithMany().HasForeignKey(h => h.ClassId)
            .HasConstraintName("fk_holds_classes_class_id")
            .OnDelete(DeleteBehavior.NoAction);

        // One active hold or admission per student, across all classes: the real guarantee behind 409.
        builder.HasIndex(h => h.StudentId)
            .HasDatabaseName("ux_holds_one_active_per_student")
            .IsUnique()
            .HasFilter("status IN ('HELD','CONFIRMED')");

        // Seat counts per class.
        builder.HasIndex(h => new { h.ClassId, h.Status, h.ExpiresAt }).HasDatabaseName("ix_holds_class_status");

        // A student's holds, newest first (GET /me/holds).
        builder.HasIndex(h => new { h.StudentId, h.CreatedAt })
            .HasDatabaseName("ix_holds_student_created")
            .IsDescending(false, true);
    }
}
