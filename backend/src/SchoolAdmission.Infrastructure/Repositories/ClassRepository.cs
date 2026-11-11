using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.Classes;
using SchoolAdmission.Domain;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Infrastructure.Repositories;

public sealed class ClassRepository(AdmissionDbContext db) : IClassRepository
{
    public async Task<SchoolClass> AddAsync(SchoolClass schoolClass, CancellationToken cancellationToken)
    {
        db.Classes.Add(schoolClass);
        // EF reads created_at (DEFAULT now()) back in the same INSERT … RETURNING statement.
        await db.SaveChangesAsync(cancellationToken);
        return schoolClass;
    }

    // One statement; ORDER BY name uses the column's numeric_ci collation.
    public async Task<IReadOnlyList<ClassDto>> ListAsync(CancellationToken cancellationToken) =>
        await ToDtos(db.Classes.AsNoTracking()
                .OrderBy(c => c.Name).ThenBy(c => c.CreatedAt).ThenBy(c => c.Id))
            .ToListAsync(cancellationToken);

    public Task<ClassDto?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        ToDtos(db.Classes.AsNoTracking().Where(c => c.Id == id)).SingleOrDefaultAsync(cancellationToken);

    // No holds table yet: nothing held or confirmed, all seats available. T-130 counts the holds here.
    private static IQueryable<ClassDto> ToDtos(IQueryable<SchoolClass> classes) =>
        classes.Select(c => new ClassDto(c.Id, c.Name, c.TotalSeats, 0, 0, c.TotalSeats));
}
