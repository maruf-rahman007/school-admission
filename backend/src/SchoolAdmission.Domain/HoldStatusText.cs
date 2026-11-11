namespace SchoolAdmission.Domain;

/// <summary>The text of each <see cref="HoldStatus"/>, as stored in <c>holds.status</c> and shown in the API.</summary>
public static class HoldStatusText
{
    public const string Held = "HELD";
    public const string Confirmed = "CONFIRMED";
    public const string Expired = "EXPIRED";
    public const string Cancelled = "CANCELLED";

    public static string ToText(HoldStatus status) => status switch
    {
        HoldStatus.Held => Held,
        HoldStatus.Confirmed => Confirmed,
        HoldStatus.Expired => Expired,
        HoldStatus.Cancelled => Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };

    public static HoldStatus FromText(string text) => text switch
    {
        Held => HoldStatus.Held,
        Confirmed => HoldStatus.Confirmed,
        Expired => HoldStatus.Expired,
        Cancelled => HoldStatus.Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(text), text, null),
    };
}
