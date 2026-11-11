namespace SchoolAdmission.Application.Holds.Commands.CreateHold;

/// <summary>How a create-hold attempt ended. The handler turns the failures into typed exceptions.</summary>
public enum CreateHoldResult
{
    Created,
    ClassNotFound,
    NoSeatsAvailable,
    StudentHasActiveHold,
}

/// <summary>Result of <c>IHoldRepository.CreateAsync</c>; <see cref="Hold"/> is set only when <see cref="Result"/> is Created.</summary>
public sealed record CreateHoldOutcome(CreateHoldResult Result, HoldCreatedDto? Hold = null);
