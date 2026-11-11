using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Domain;

namespace SchoolAdmission.Infrastructure.Persistence;

/// <summary>EF Core context for the admission database.</summary>
public sealed class AdmissionDbContext(DbContextOptions<AdmissionDbContext> options) : DbContext(options)
{
    /// <summary>
    /// ICU collation for class names: digits compare by numeric value (<c>kn</c>) and case is ignored
    /// (<c>ks-level2</c>). Non-deterministic, so names that differ only in case compare equal and the
    /// list falls back to its tie-breakers (created_at, id).
    /// </summary>
    public const string NumericCaseInsensitiveCollation = "numeric_ci";

    public DbSet<SchoolClass> Classes => Set<SchoolClass>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasCollation(
            NumericCaseInsensitiveCollation, locale: "und-u-kn-ks-level2", provider: "icu", deterministic: false);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdmissionDbContext).Assembly);
    }
}
