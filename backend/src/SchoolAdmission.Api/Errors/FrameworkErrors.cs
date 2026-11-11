using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SchoolAdmission.Api.Errors;

/// <summary>
/// Puts errors that the framework produces without an exception (model binding, unknown route, wrong method,
/// wrong content type, bare status results) into the standard error body.
/// </summary>
public static class FrameworkErrors
{
    /// <summary>
    /// Replaces the [ApiController] automatic 400. With implicit [Required] turned off, ModelState only holds
    /// binding/JSON errors, so every entry here means the body could not be read: MALFORMED_REQUEST.
    /// Missing or invalid field values are reported by FluentValidation as VALIDATION_FAILED instead.
    /// </summary>
    public static IActionResult MalformedRequest(ActionContext context)
    {
        var description = string.Join(" ", context.ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => string.IsNullOrEmpty(error.ErrorMessage) ? error.Exception?.Message : error.ErrorMessage));

        var body = new ErrorResponse(
            new UserResponse(ErrorCodes.MalformedRequest, "The request body is not valid JSON or has wrong types.", null, []),
            new DevResponse(null, description, ErrorResponseWriter.TraceId(context.HttpContext), null));

        return new ObjectResult(body)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/json" },
        };
    }

    /// <summary>Status-code-pages handler: wraps any empty 4xx/5xx response in the standard body.</summary>
    public static Task WriteForStatusCode(StatusCodeContext context)
    {
        var http = context.HttpContext;
        var (code, message) = http.Response.StatusCode switch
        {
            StatusCodes.Status400BadRequest => (ErrorCodes.MalformedRequest, "The request was invalid."),
            StatusCodes.Status404NotFound => (ErrorCodes.RouteNotFound, "No endpoint matches this URL."),
            StatusCodes.Status405MethodNotAllowed => (ErrorCodes.MethodNotAllowed, "This HTTP method is not allowed here."),
            StatusCodes.Status415UnsupportedMediaType =>
                (ErrorCodes.UnsupportedMediaType, "The request body must be sent as application/json."),
            _ => (ErrorCodes.InternalError, "Something went wrong on the server."),
        };

        return ErrorResponseWriter.WriteAsync(
            http, http.Response.StatusCode, code, message,
            devMessage: $"{http.Response.StatusCode} returned without a body for {http.Request.Method} {http.Request.Path}.");
    }
}
