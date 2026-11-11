namespace SchoolAdmission.Application.Holds.Commands.CancelHold;

/// <summary>How a cancel attempt ended. The handler turns the failures into typed exceptions.</summary>
public enum CancelHoldResult
{
    /// <summary>HELD → CANCELLED by this request.</summary>
    Cancelled,

    /// <summary>Already CANCELLED or EXPIRED (an expired HELD row is written EXPIRED): 204, idempotent.</summary>
    Unchanged,
    NotFound,
    NotOwner,
    Confirmed,
}
