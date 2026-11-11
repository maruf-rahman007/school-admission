using SchoolAdmission.Application.Common.Errors;

namespace SchoolAdmission.Application.Holds;

/// <summary>409 HOLD_CANCELLED: a cancelled hold cannot be confirmed (410 is kept for expired holds only).</summary>
public sealed class HoldCancelledException(string requestedId)
    : AppException("HOLD_CANCELLED", 409, "This hold was cancelled.", requestedId);
