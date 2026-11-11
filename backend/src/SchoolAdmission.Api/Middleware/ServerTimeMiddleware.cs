using System.Globalization;

namespace SchoolAdmission.Api.Middleware;

/// <summary>
/// Adds <c>X-Server-Time</c> (UTC, milliseconds) to every response so the frontend can correct for a wrong
/// client clock. Display only: expiry is always decided by the database clock.
/// </summary>
public sealed class ServerTimeMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Server-Time";

    /// <summary>The current server time as ISO-8601 UTC with milliseconds, e.g. <c>2026-11-11T10:15:30.123Z</c>.
    /// Also used for the <c>serverTime</c> field of object responses.</summary>
    public static string Now() =>
        DateTimeOffset.UtcNow.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    public Task InvokeAsync(HttpContext context)
    {
        // OnStarting runs just before the headers are sent, so the header survives short-circuits
        // (e.g. CORS preflight) and handlers that clear the response.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = Now();
            return Task.CompletedTask;
        });

        return next(context);
    }
}
