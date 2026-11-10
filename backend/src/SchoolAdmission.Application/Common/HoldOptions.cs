namespace SchoolAdmission.Application.Common;

/// <summary>Hold timing settings. Bound and validated at startup by the Api project.</summary>
public sealed class HoldOptions
{
    public required int HoldDurationSeconds { get; init; }

    public required int ExpirySweepIntervalSeconds { get; init; }
}
