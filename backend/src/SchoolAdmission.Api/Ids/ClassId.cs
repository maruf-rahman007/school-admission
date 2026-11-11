namespace SchoolAdmission.Api.Ids;

/// <summary>
/// Public class IDs: <c>c_</c> + the UUID as 32 hex characters (ARCHITECTURE §3, "IDs").
/// The prefix exists only at the API boundary; inner layers use the plain <see cref="Guid"/>.
/// </summary>
public static class ClassId
{
    public const string Prefix = "c_";

    /// <summary>Formats as <c>c_</c> + 32 lowercase hex characters.</summary>
    public static string Format(Guid id) => PrefixedId.Format(Prefix, id);

    /// <summary>
    /// Accepts exactly <c>c_</c> + 32 ASCII hex characters (either case). Anything else, including dashed or braced
    /// UUIDs, other prefixes, whitespace or non-ASCII digits, is not a class ID. Never throws.
    /// </summary>
    public static bool TryParse(string? value, out Guid id) => PrefixedId.TryParse(Prefix, value, out id);
}
