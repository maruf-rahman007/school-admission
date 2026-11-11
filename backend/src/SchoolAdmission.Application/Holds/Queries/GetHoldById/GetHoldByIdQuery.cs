namespace SchoolAdmission.Application.Holds.Queries.GetHoldById;

/// <summary>Read one hold. Anyone may read it (no student ID needed). No validator: the Api parses the ID.</summary>
public sealed record GetHoldByIdQuery(Guid Id);
