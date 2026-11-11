using System.Text.Json;
using FluentValidation;
using SchoolAdmission.Application.Common.Errors;

namespace SchoolAdmission.Api.Errors;

/// <summary>
/// Turns every exception into the standard error body: <see cref="AppException"/> keeps its code and status,
/// FluentValidation failures become 400 VALIDATION_FAILED, anything else becomes 500 INTERNAL_ERROR and is logged with the trace id.
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    private const string InternalErrorMessage = "Something went wrong on the server.";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var traceId = ErrorResponseWriter.TraceId(context);

        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            // The client went away (e.g. closed the tab): not a server fault, and nobody is left to answer.
            logger.LogInformation("Request aborted by the client. TraceId {TraceId}", traceId);
            return;
        }

        if (context.Response.HasStarted)
        {
            // Headers are already sent: the body can't be replaced, so log and end the broken response.
            logger.LogError(exception, "Unhandled exception after the response started. TraceId {TraceId}", traceId);
            context.Abort();
            return;
        }

        if (exception is ValidationException validation)
        {
            var errors = validation.Errors
                .Select(e => new ErrorItem(JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName), e.ErrorCode, e.ErrorMessage))
                .ToList();
            await ErrorResponseWriter.WriteAsync(
                context, StatusCodes.Status400BadRequest, ErrorCodes.ValidationFailed, "Validation failed.",
                errors: errors, exception: exception);
            return;
        }

        if (exception is AppException app)
        {
            var errors = app.Errors.Select(e => new ErrorItem(e.Field, e.Code, e.Message)).ToList();
            await ErrorResponseWriter.WriteAsync(
                context, app.Status, app.Code, app.Message, app.ResourceId, errors, exception);
            return;
        }

        logger.LogError(exception, "Unhandled exception. TraceId {TraceId}", traceId);
        await ErrorResponseWriter.WriteAsync(
            context, StatusCodes.Status500InternalServerError, ErrorCodes.InternalError, InternalErrorMessage,
            exception: exception);
    }
}
