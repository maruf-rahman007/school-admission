using System.Globalization;

namespace SchoolAdmission.Api.Contracts;

/// <summary>The one timestamp format of the API: ISO-8601 UTC with milliseconds and <c>Z</c>, e.g. <c>2026-11-11T10:15:30.123Z</c>.</summary>
public static class ApiTime
{
    public static string Format(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
}
