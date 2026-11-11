using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.Application.Classes.Queries.GetClassById;

public sealed class GetClassByIdHandler(IClassRepository classes)
{
    /// <summary>The class, or null when no class has this id (the caller answers 404 CLASS_NOT_FOUND).</summary>
    public Task<ClassDto?> Handle(GetClassByIdQuery query, CancellationToken cancellationToken) =>
        classes.FindAsync(query.Id, cancellationToken);
}
