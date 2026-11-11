namespace SchoolAdmission.Application.Holds.Commands.CancelHold;

/// <summary>
/// Cancel a hold. <see cref="StudentId"/> is already validated by the Api's <c>[RequireStudentId]</c> filter.
/// <see cref="RequestedHoldId"/> is the hold ID as the client sent it, used as <c>resourceId</c> in error bodies.
/// </summary>
public sealed record CancelHoldCommand(Guid HoldId, string StudentId, string RequestedHoldId);
