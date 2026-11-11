using Microsoft.AspNetCore.Mvc.Filters;
using SchoolAdmission.Application.Common.Errors;

namespace SchoolAdmission.Api.Filters;

/// <summary>
/// Requires a valid <c>X-Student-Id</c> header: exactly one value of exactly 17 ASCII digits (DECISIONS Q7).
/// Otherwise the action does not run and the client gets 400 INVALID_STUDENT_ID.
/// The validated ID is read in the action with <see cref="StudentIdHttpContextExtensions.GetStudentId"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireStudentIdAttribute : ActionFilterAttribute
{
    public const string HeaderName = "X-Student-Id";
    public const string ErrorCode = "INVALID_STUDENT_ID";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var values = context.HttpContext.Request.Headers[HeaderName];

        // Kept as a string so leading zeros survive; no trimming (strict, as decided).
        if (values.Count != 1 || !IsValid(values[0]))
        {
            throw new AppException(
                ErrorCode, StatusCodes.Status400BadRequest,
                $"The {HeaderName} header must be exactly 17 digits (0-9).");
        }

        context.HttpContext.Items[StudentIdHttpContextExtensions.ItemKey] = values[0];
    }

    // ASCII 0-9 only: char.IsDigit or \d would also accept Bangla and other Unicode digits.
    private static bool IsValid(string? value) =>
        value is { Length: 17 } && value.All(c => c is >= '0' and <= '9');
}

public static class StudentIdHttpContextExtensions
{
    internal const string ItemKey = "SchoolAdmission.StudentId";

    /// <summary>The ID validated by <see cref="RequireStudentIdAttribute"/>. Only valid on actions that carry it.</summary>
    public static string GetStudentId(this HttpContext context) =>
        context.Items[ItemKey] as string
        ?? throw new InvalidOperationException($"{nameof(RequireStudentIdAttribute)} is missing on this action.");
}
