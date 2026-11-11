using SchoolAdmission.Application.Common.Errors;

namespace SchoolAdmission.Application.Holds;

/// <summary>409 HOLD_ALREADY_CONFIRMED: a confirmed admission is final and cannot be cancelled.</summary>
public sealed class HoldAlreadyConfirmedException(string requestedId)
    : AppException("HOLD_ALREADY_CONFIRMED", 409, "This hold is already confirmed and cannot be cancelled.", requestedId);
