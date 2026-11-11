using SchoolAdmission.Application.Common.Errors;

namespace SchoolAdmission.Application.Holds;

/// <summary>409 STUDENT_HAS_ACTIVE_HOLD: the student already has a HELD or CONFIRMED hold in some class.</summary>
public sealed class StudentHasActiveHoldException()
    : AppException("STUDENT_HAS_ACTIVE_HOLD", 409, "The student already has an active application.");
