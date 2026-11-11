using SchoolAdmission.Application.Common.Errors;

namespace SchoolAdmission.Application.Holds;

/// <summary>404 HOLD_NOT_FOUND: the hold ID is unknown or cannot be parsed (both answered the same way).</summary>
public sealed class HoldNotFoundException(string requestedId)
    : AppException("HOLD_NOT_FOUND", 404, "Hold not found.", requestedId);
