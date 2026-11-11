using SchoolAdmission.Domain;

namespace SchoolAdmission.Application.Holds.Commands.CreateHold;

/// <summary>A newly created hold (always HELD); <see cref="ExpiresAt"/> comes from the database clock.</summary>
public sealed record HoldCreatedDto(Guid HoldId, Guid ClassId, string StudentId, HoldStatus Status, DateTimeOffset ExpiresAt);
