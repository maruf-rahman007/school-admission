namespace SchoolAdmission.Application.Holds.Commands.CreateHold;

/// <summary>
/// Apply for a seat. <see cref="StudentId"/> is already validated (17 digits) by the Api's <c>[RequireStudentId]</c> filter,
/// so there is no validator. <see cref="RequestedClassId"/> is the class ID as the client sent it, used as
/// <c>resourceId</c> in the 404/409 error bodies.
/// </summary>
public sealed record CreateHoldCommand(Guid ClassId, string StudentId, string RequestedClassId);
