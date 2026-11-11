using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.Classes;
using SchoolAdmission.Application.Common;

namespace SchoolAdmission.Application.Holds.Commands.CreateHold;

public sealed class CreateHoldHandler(IHoldRepository holds, HoldOptions options)
{
    /// <summary>
    /// Holds a seat for the student, or throws: 404 CLASS_NOT_FOUND, 409 NO_SEATS_AVAILABLE, 409 STUDENT_HAS_ACTIVE_HOLD.
    /// All rules are enforced inside PostgreSQL (row lock + partial unique index), so this is safe across processes.
    /// </summary>
    public async Task<HoldCreatedDto> Handle(CreateHoldCommand command, CancellationToken cancellationToken)
    {
        var outcome = await holds.CreateAsync(
            command.ClassId, command.StudentId, options.HoldDurationSeconds, cancellationToken);

        return outcome.Result switch
        {
            CreateHoldResult.Created => outcome.Hold!,
            CreateHoldResult.ClassNotFound => throw new ClassNotFoundException(command.RequestedClassId),
            CreateHoldResult.NoSeatsAvailable => throw new NoSeatsAvailableException(command.RequestedClassId),
            CreateHoldResult.StudentHasActiveHold => throw new StudentHasActiveHoldException(),
            _ => throw new InvalidOperationException($"Unknown create-hold result {outcome.Result}."),
        };
    }
}
