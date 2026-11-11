using SchoolAdmission.Domain;

namespace SchoolAdmission.Application.Classes.Commands.CreateClass;

/// <summary>A class with its seat counts (ARCHITECTURE §4).</summary>
public sealed record ClassDto(Guid Id, string Name, int TotalSeats, int HeldSeats, int ConfirmedSeats, int AvailableSeats)
{
    /// <summary>
    /// Counts while no holds exist yet: nothing held or confirmed, every seat available.
    /// T-130 replaces this with counts computed from the holds table.
    /// </summary>
    public static ClassDto WithoutHolds(SchoolClass schoolClass) =>
        new(schoolClass.Id, schoolClass.Name, schoolClass.TotalSeats, 0, 0, schoolClass.TotalSeats);
}
