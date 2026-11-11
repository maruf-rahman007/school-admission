using SchoolAdmission.Domain;

namespace SchoolAdmission.Application.Abstractions;

/// <summary>Persistence for classes; implemented in Infrastructure with EF Core.</summary>
public interface IClassRepository
{
    /// <summary>Inserts the class and returns it with the database-generated values (e.g. <c>CreatedAt</c>).</summary>
    Task<SchoolClass> AddAsync(SchoolClass schoolClass, CancellationToken cancellationToken);
}
