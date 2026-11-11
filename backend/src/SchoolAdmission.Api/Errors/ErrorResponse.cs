namespace SchoolAdmission.Api.Errors;

/// <summary>The body of every non-2xx response (STANDARDS §5).</summary>
public sealed record ErrorResponse(UserResponse UserResponse, DevResponse DevResponse);

/// <summary>What the client shows: a stable code plus English text. <c>errors</c> is always an array.</summary>
public sealed record UserResponse(string Code, string Message, string? ResourceId, IReadOnlyList<ErrorItem> Errors);

public sealed record ErrorItem(string Field, string Code, string Message);

/// <summary>Diagnostics, always included (DECISIONS Q10b). Exception fields are null when no exception exists.</summary>
public sealed record DevResponse(string? ExceptionType, string? Message, string TraceId, string? StackTrace);
