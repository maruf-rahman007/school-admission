using SchoolAdmission.Application.Classes;
using SchoolAdmission.Domain;

namespace SchoolAdmission.Application.Abstractions;

/// <summary>Persistence for classes; implemented in Infrastructure with EF Core.</summary>
public interface IClassRepository
{
    /// <summary>Inserts the class and returns it with the database-generated values (e.g. <c>CreatedAt</c>).</summary>
    Task<SchoolClass> AddAsync(SchoolClass schoolClass, CancellationToken cancellationToken);

    /// <summary>Every class with its seat counts, sorted by the database: name (collation), created_at, id.</summary>
    Task<IReadOnlyList<ClassDto>> ListAsync(CancellationToken cancellationToken);

    /// <summary>One class with its seat counts, or null.</summary>
    Task<ClassDto?> FindAsync(Guid id, CancellationToken cancellationToken);
}
