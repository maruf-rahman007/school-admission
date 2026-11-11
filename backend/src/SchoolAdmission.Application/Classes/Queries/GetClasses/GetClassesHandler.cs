using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.Application.Classes.Queries.GetClasses;

public sealed class GetClassesHandler(IClassRepository classes)
{
    /// <summary>All classes, sorted by name (number-aware, case-insensitive), then oldest first, then id.</summary>
    public Task<IReadOnlyList<ClassDto>> Handle(GetClassesQuery query, CancellationToken cancellationToken) =>
        classes.ListAsync(cancellationToken);
}
