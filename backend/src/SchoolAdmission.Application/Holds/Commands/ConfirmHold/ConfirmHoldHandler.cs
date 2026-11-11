using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.Application.Holds.Commands.ConfirmHold;

public sealed class ConfirmHoldHandler(IHoldRepository holds)
{
    /// <summary>
    /// Confirms the student's own HELD, unexpired hold, or returns it unchanged if it is already CONFIRMED.
    /// Throws 404 HOLD_NOT_FOUND, 403 NOT_HOLD_OWNER, 409 HOLD_CANCELLED or 410 HOLD_EXPIRED.
    /// The hold row is locked, so parallel confirms count the seat once (ARCHITECTURE §5.2).
    /// </summary>
    public async Task<HoldDto> Handle(ConfirmHoldCommand command, CancellationToken cancellationToken)
    {
        var outcome = await holds.ConfirmAsync(command.HoldId, command.StudentId, cancellationToken);

        return outcome.Result switch
        {
            ConfirmHoldResult.Confirmed => outcome.Hold!,
            ConfirmHoldResult.NotFound => throw new HoldNotFoundException(command.RequestedHoldId),
            ConfirmHoldResult.NotOwner => throw new NotHoldOwnerException(command.RequestedHoldId),
            ConfirmHoldResult.Cancelled => throw new HoldCancelledException(command.RequestedHoldId),
            ConfirmHoldResult.Expired => throw new HoldExpiredException(command.RequestedHoldId),
            _ => throw new InvalidOperationException($"Unknown confirm-hold result {outcome.Result}."),
        };
    }
}
