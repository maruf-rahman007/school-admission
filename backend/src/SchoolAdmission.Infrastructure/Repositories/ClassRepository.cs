using SchoolAdmission.Application.Abstractions;
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
}
