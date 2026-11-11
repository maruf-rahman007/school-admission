using SchoolAdmission.Application.Common.Errors;

namespace SchoolAdmission.Application.Holds;

/// <summary>403 NOT_HOLD_OWNER: the student in <c>X-Student-Id</c> tries to confirm or cancel someone else's hold.</summary>
public sealed class NotHoldOwnerException(string requestedId)
    : AppException("NOT_HOLD_OWNER", 403, "This hold belongs to another student.", requestedId);
