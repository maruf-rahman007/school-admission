namespace SchoolAdmission.Application.Holds.Commands.ConfirmHold;

/// <summary>How a confirm attempt ended. The handler turns the failures into typed exceptions.</summary>
public enum ConfirmHoldResult
{
    /// <summary>Now CONFIRMED: either confirmed by this request or already confirmed before (idempotent).</summary>
    Confirmed,
    NotFound,
    NotOwner,
    Cancelled,
    Expired,
}

/// <summary>Result of <c>IHoldRepository.ConfirmAsync</c>; <see cref="Hold"/> is set only when <see cref="Result"/> is Confirmed.</summary>
public sealed record ConfirmHoldOutcome(ConfirmHoldResult Result, HoldDto? Hold = null);
