namespace SchoolAdmission.Api.Contracts;

/// <summary>Body of <c>POST /classes</c>. Nullable so missing fields are reported as REQUIRED by the validator.</summary>
public sealed class CreateClassRequest
{
    public string? Name { get; init; }

    public int? TotalSeats { get; init; }
}
