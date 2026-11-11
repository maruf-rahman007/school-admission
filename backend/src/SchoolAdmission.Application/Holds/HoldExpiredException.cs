using SchoolAdmission.Application.Common.Errors;

namespace SchoolAdmission.Application.Holds;

/// <summary>410 HOLD_EXPIRED: the hold ran out (database clock) before it was confirmed.</summary>
public sealed class HoldExpiredException(string requestedId)
    : AppException("HOLD_EXPIRED", 410, "This hold has expired.", requestedId);
