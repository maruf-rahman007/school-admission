using Microsoft.EntityFrameworkCore;

namespace SchoolAdmission.Infrastructure.Persistence;

/// <summary>EF Core context for the admission database. Tables are added by the feature tickets.</summary>
public sealed class AdmissionDbContext(DbContextOptions<AdmissionDbContext> options) : DbContext(options);
