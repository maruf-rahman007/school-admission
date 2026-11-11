using SchoolAdmission.Application.Common.Errors;

namespace SchoolAdmission.Application.Holds;

/// <summary>409 NO_SEATS_AVAILABLE: held + confirmed already equals the class's total seats.</summary>
public sealed class NoSeatsAvailableException(string requestedClassId)
    : AppException("NO_SEATS_AVAILABLE", 409, "No seats are available in this class.", requestedClassId);
