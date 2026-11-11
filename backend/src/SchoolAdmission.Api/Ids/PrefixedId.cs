namespace SchoolAdmission.Api.Ids;

/// <summary>Shared format/parse rules for prefixed IDs: prefix + 32 hex characters.</summary>
internal static class PrefixedId
{
    private const int HexLength = 32;

    public static string Format(string prefix, Guid id) => prefix + id.ToString("N");

    /// <summary>
    /// Accepts exactly <paramref name="prefix"/> + 32 ASCII hex characters (either case). Anything else, including dashed or
    /// braced UUIDs, other prefixes, whitespace or non-ASCII digits, is rejected. Never throws.
    /// </summary>
    public static bool TryParse(string prefix, string? value, out Guid id)
    {
        id = Guid.Empty;
        if (value is null
            || value.Length != prefix.Length + HexLength
            || !value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var hex = value.AsSpan(prefix.Length);
        foreach (var c in hex)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return false;
            }
        }

        return Guid.TryParseExact(hex, "N", out id);
    }
}
