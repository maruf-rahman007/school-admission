namespace SchoolAdmission.Api.Ids;

/// <summary>
/// Public hold IDs: <c>h_</c> + the UUID as 32 hex characters (ARCHITECTURE §3, "IDs"); same rules as <see cref="ClassId"/>.
/// The prefix exists only at the API boundary; inner layers use the plain <see cref="Guid"/>.
/// </summary>
public static class HoldId
{
    public const string Prefix = "h_";

    /// <summary>Formats as <c>h_</c> + 32 lowercase hex characters.</summary>
    public static string Format(Guid id) => PrefixedId.Format(Prefix, id);

    /// <summary>Accepts exactly <c>h_</c> + 32 ASCII hex characters (either case). Never throws.</summary>
    public static bool TryParse(string? value, out Guid id) => PrefixedId.TryParse(Prefix, value, out id);
}
