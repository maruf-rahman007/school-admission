using SchoolAdmission.Domain;

namespace SchoolAdmission.Application.Classes;

/// <summary>A class with its seat counts (ARCHITECTURE §4).</summary>
public sealed record ClassDto(Guid Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats)
{
    /// <summary>Counts of a class that has no holds yet (a newly created one): nothing held or confirmed, every seat available.</summary>
    public static ClassDto WithoutHolds(SchoolClass schoolClass) =>
        new(schoolClass.Id, schoolClass.Name, schoolClass.TotalSeats, 0, 0, schoolClass.TotalSeats);
}
