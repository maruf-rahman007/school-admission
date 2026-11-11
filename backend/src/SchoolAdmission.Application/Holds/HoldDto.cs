using SchoolAdmission.Domain;

namespace SchoolAdmission.Application.Holds;

/// <summary>A hold as read back: <see cref="Status"/> is the effective status (a HELD hold past its expiry reads EXPIRED).</summary>
public sealed record HoldDto(Guid Id, Guid ClassId, string ClassName, HoldStatus Status, DateTimeOffset ExpiresAt);
