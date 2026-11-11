namespace SchoolAdmission.Application.Holds.Queries.GetMyHolds;

/// <summary>Every hold of one student. No validator: <c>[RequireStudentId]</c> has already checked the header.</summary>
public sealed record GetMyHoldsQuery(string StudentId);
