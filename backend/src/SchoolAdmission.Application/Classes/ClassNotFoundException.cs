using SchoolAdmission.Application.Common.Errors;

namespace SchoolAdmission.Application.Classes;

/// <summary>404 CLASS_NOT_FOUND: the class ID is unknown or cannot be parsed (both answered the same way).</summary>
public sealed class ClassNotFoundException(string requestedId)
    : AppException("CLASS_NOT_FOUND", 404, "Class not found.", requestedId);
