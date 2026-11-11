namespace SchoolAdmission.Application.Common.Errors;

/// <summary>One field-level problem, reported in <c>userResponse.errors[]</c>.</summary>
public sealed record FieldError(string Field, string Code, string Message);

/// <summary>
/// A client-facing failure with a stable error <see cref="Code"/> and HTTP <see cref="Status"/>.
/// The global error middleware turns it into the standard error body (STANDARDS §5).
/// Typed business exceptions (e.g. <c>NoSeatsAvailableException</c>) derive from it.
/// </summary>
public class AppException : Exception
{
    public AppException(
        string code,
        int status,
        string message,
        string? resourceId = null,
        IReadOnlyList<FieldError>? errors = null)
        : base(message)
    {
        Code = code;
        Status = status;
        ResourceId = resourceId;
        Errors = errors ?? [];
    }

    /// <summary>UPPER_SNAKE_CASE code from ARCHITECTURE §7; the frontend translates by it.</summary>
    public string Code { get; }

    /// <summary>HTTP status code (a plain int so Application needs no ASP.NET reference).</summary>
    public int Status { get; }

    public string? ResourceId { get; }

    public IReadOnlyList<FieldError> Errors { get; }
}
