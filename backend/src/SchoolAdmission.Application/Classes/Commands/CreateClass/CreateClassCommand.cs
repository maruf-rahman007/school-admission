namespace SchoolAdmission.Application.Classes.Commands.CreateClass;

/// <summary>Both values are nullable so a missing field reaches the validator (REQUIRED) instead of defaulting.</summary>
public sealed record CreateClassCommand(string? Name, int? TotalSeats);
