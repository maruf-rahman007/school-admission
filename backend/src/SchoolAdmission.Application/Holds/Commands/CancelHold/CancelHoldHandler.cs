using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.Application.Holds.Commands.CancelHold;

public sealed class CancelHoldHandler(IHoldRepository holds)
{
    /// <summary>
    /// Cancels the student's own HELD hold; an already cancelled or expired hold is left as it is (idempotent).
    /// Throws 404 HOLD_NOT_FOUND, 403 NOT_HOLD_OWNER or 409 HOLD_ALREADY_CONFIRMED.
    /// </summary>
    public async Task Handle(CancelHoldCommand command, CancellationToken cancellationToken)
    {
        var result = await holds.CancelAsync(command.HoldId, command.StudentId, cancellationToken);

        switch (result)
        {
            case CancelHoldResult.Cancelled:
            case CancelHoldResult.Unchanged:
                return;
            case CancelHoldResult.NotFound:
                throw new HoldNotFoundException(command.RequestedHoldId);
            case CancelHoldResult.NotOwner:
                throw new NotHoldOwnerException(command.RequestedHoldId);
            case CancelHoldResult.Confirmed:
                throw new HoldAlreadyConfirmedException(command.RequestedHoldId);
            default:
                throw new InvalidOperationException($"Unknown cancel-hold result {result}.");
        }
    }
}
