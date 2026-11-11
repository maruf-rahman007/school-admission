using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.Application.Holds.Queries.GetMyHolds;

public sealed class GetMyHoldsHandler(IHoldRepository holds)
{
    /// <summary>All of the student's holds, newest first, with their effective status (empty when there are none).</summary>
    public Task<IReadOnlyList<HoldDto>> Handle(GetMyHoldsQuery query, CancellationToken cancellationToken) =>
        holds.ListByStudentAsync(query.StudentId, cancellationToken);
}
