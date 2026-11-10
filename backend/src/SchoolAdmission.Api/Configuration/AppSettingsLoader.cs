using System.Globalization;
using SchoolAdmission.Application.Common;

namespace SchoolAdmission.Api.Configuration;

/// <summary>
/// Reads the backend settings (environment variables over appsettings.json defaults) and validates them.
/// Every problem is collected; if there is any, <see cref="InvalidConfigurationException"/> is thrown.
/// </summary>
public static class AppSettingsLoader
{
    public const string ConnectionStringVariable = "ConnectionStrings__Default";
    public const string HoldDurationVariable = "HOLD_DURATION_SECONDS";
    public const string SweepIntervalVariable = "EXPIRY_SWEEP_INTERVAL_SECONDS";
    public const string CorsOriginsVariable = "CORS_ALLOWED_ORIGINS";

    public static AppSettings Load(IConfiguration configuration)
    {
        var errors = new List<string>();

        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            // Never print the value: it may contain a password.
            errors.Add($"{ConnectionStringVariable} must not be empty.");
        }

        var holdDuration = ReadPositiveInt(configuration, HoldDurationVariable, errors);
        var sweepInterval = ReadPositiveInt(configuration, SweepIntervalVariable, errors);
        var corsOrigins = ReadCorsOrigins(configuration[CorsOriginsVariable], errors);

        if (errors.Count > 0)
        {
            throw new InvalidConfigurationException(errors);
        }

        return new AppSettings
        {
            ConnectionString = connectionString!,
            Hold = new HoldOptions { HoldDurationSeconds = holdDuration, ExpirySweepIntervalSeconds = sweepInterval },
            CorsAllowedOrigins = corsOrigins,
        };
    }

    private static int ReadPositiveInt(IConfiguration configuration, string variable, List<string> errors)
    {
        var raw = configuration[variable];
        // NumberStyles.Integer allows surrounding spaces and a leading sign; only ASCII digits parse.
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0)
        {
            return value;
        }

        errors.Add($"{variable} must be a positive whole number, but was '{raw}'.");
        return 0;
    }

    private static List<string> ReadCorsOrigins(string? raw, List<string> errors)
    {
        var origins = new List<string>();
        var entries = (raw ?? string.Empty)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in entries)
        {
            var origin = entry.TrimEnd('/');
            if (IsValidOrigin(origin))
            {
                origins.Add(origin);
            }
            else
            {
                errors.Add($"{CorsOriginsVariable} entry '{entry}' must be an http(s) URL without a path, query, user info or fragment, " +
                    "e.g. http://localhost:3000.");
            }
        }

        if (entries.Length == 0)
        {
            errors.Add($"{CorsOriginsVariable} must contain at least one origin, but was '{raw}'.");
        }

        return origins;
    }

    private static bool IsValidOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && uri.AbsolutePath == "/"
        && uri.Query.Length == 0
        && uri.Fragment.Length == 0
        && uri.UserInfo.Length == 0;
}
