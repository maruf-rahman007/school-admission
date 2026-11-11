namespace SchoolAdmission.Domain;

/// <summary>
/// A seat held for one student in one class. It expires at <see cref="ExpiresAt"/> unless confirmed first.
/// Expiry is always judged against the database clock, never in C# (ADR-0002).
/// </summary>
public sealed class Hold
{
    public Guid Id { get; init; }

    public Guid ClassId { get; init; }

    /// <summary>17-digit birth registration number, kept as text so leading zeros survive.</summary>
    public required string StudentId { get; init; }

    public HoldStatus Status { get; init; }

    /// <summary>Set by the database (<c>now()</c>) when the row is inserted.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset ExpiresAt { get; init; }

    public DateTimeOffset? ConfirmedAt { get; init; }

    public DateTimeOffset? CancelledAt { get; init; }
}
