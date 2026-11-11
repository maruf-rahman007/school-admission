using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.Application.Holds.Queries.GetHoldById;

public sealed class GetHoldByIdHandler(IHoldRepository holds)
{
    /// <summary>The hold, or null when no hold has this id (the caller answers 404 HOLD_NOT_FOUND).</summary>
    public Task<HoldDto?> Handle(GetHoldByIdQuery query, CancellationToken cancellationToken) =>
        holds.FindAsync(query.Id, cancellationToken);
}
