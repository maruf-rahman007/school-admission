namespace SchoolAdmission.Api.Configuration;

/// <summary>Thrown at startup when one or more settings are invalid, so the backend refuses to start.</summary>
public sealed class InvalidConfigurationException(IReadOnlyList<string> errors)
    : Exception("Invalid configuration, the backend will not start:" + Environment.NewLine
        + string.Join(Environment.NewLine, errors.Select(e => " - " + e)))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
