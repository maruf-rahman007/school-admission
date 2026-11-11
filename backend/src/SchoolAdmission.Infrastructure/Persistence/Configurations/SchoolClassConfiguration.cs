using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolAdmission.Domain;

namespace SchoolAdmission.Infrastructure.Persistence.Configurations;

/// <summary>Maps <see cref="SchoolClass"/> to the <c>classes</c> table (ARCHITECTURE §3).</summary>
public sealed class SchoolClassConfiguration : IEntityTypeConfiguration<SchoolClass>
{
    public void Configure(EntityTypeBuilder<SchoolClass> builder)
    {
        builder.ToTable("classes", table =>
            table.HasCheckConstraint("ck_classes_total_seats_positive", "total_seats > 0"));

        builder.HasKey(c => c.Id).HasName("pk_classes");
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();

        // Number-aware, case-insensitive sort order: "Class 6" < "Class 7" < "class 8" < "Class 10".
        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .UseCollation(AdmissionDbContext.NumericCaseInsensitiveCollation)
            .IsRequired();

        builder.Property(c => c.TotalSeats).HasColumnName("total_seats").IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd();
    }
}
