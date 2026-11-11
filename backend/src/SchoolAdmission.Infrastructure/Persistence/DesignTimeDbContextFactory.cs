using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SchoolAdmission.Infrastructure.Persistence;

/// <summary>
/// Used only by the <c>dotnet ef</c> tool to create migrations. Adding a migration needs no database,
/// so the connection string here is a placeholder and is never used at runtime.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AdmissionDbContext>
{
    public AdmissionDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AdmissionDbContext>().UseNpgsql("Host=localhost;Database=design_time").Options);
}
