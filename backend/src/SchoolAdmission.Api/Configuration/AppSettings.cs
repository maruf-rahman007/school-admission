using SchoolAdmission.Application.Common;

namespace SchoolAdmission.Api.Configuration;

/// <summary>All backend settings, validated. Built by <see cref="AppSettingsLoader"/>.</summary>
public sealed class AppSettings
{
    public required string ConnectionString { get; init; }

    public required HoldOptions Hold { get; init; }

    public required IReadOnlyList<string> CorsAllowedOrigins { get; init; }
}
