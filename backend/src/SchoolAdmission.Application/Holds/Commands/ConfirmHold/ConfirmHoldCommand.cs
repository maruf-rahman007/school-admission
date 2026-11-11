namespace SchoolAdmission.Application.Holds.Commands.ConfirmHold;

/// <summary>
/// Pay for (confirm) a hold. <see cref="StudentId"/> is already validated by the Api's <c>[RequireStudentId]</c> filter.
/// <see cref="RequestedHoldId"/> is the hold ID as the client sent it, used as <c>resourceId</c> in error bodies.
/// </summary>
public sealed record ConfirmHoldCommand(Guid HoldId, string StudentId, string RequestedHoldId);
