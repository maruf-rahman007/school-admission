using System.Diagnostics;
using System.Text.Json;

namespace SchoolAdmission.Api.Errors;

/// <summary>Writes the standard error body. Used by the error middleware and the status-code fallback.</summary>
public static class ErrorResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The id shared by the response body and the log entries of this request.</summary>
    public static string TraceId(HttpContext context) => Activity.Current?.Id ?? context.TraceIdentifier;

    public static Task WriteAsync(
        HttpContext context,
        int status,
        string code,
        string message,
        string? resourceId = null,
        IReadOnlyList<ErrorItem>? errors = null,
        Exception? exception = null,
        string? devMessage = null)
    {
        var body = new ErrorResponse(
            new UserResponse(code, message, resourceId, errors ?? []),
            new DevResponse(
                exception?.GetType().Name,
                exception?.Message ?? devMessage,
                TraceId(context),
                exception?.StackTrace));

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions), context.RequestAborted);
    }
}
