namespace SchoolAdmission.Domain;

/// <summary>
/// One class/shift that parents can apply to (e.g. "Class 6 – Morning Shift"), with its own seat count.
/// Names are not unique; the seat count is always positive.
/// </summary>
public sealed class SchoolClass
{
    public Guid Id { get; init; }

    public required string Name { get; init; }

    public int TotalSeats { get; init; }

    /// <summary>Set by the database (<c>now()</c>) when the row is inserted.</summary>
    public DateTimeOffset CreatedAt { get; init; }
}
