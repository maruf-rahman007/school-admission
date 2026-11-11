using FluentValidation;
using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Domain;

namespace SchoolAdmission.Application.Classes.Commands.CreateClass;

public sealed class CreateClassHandler(IValidator<CreateClassCommand> validator, IClassRepository classes)
{
    /// <summary>Validates (failure → 400 VALIDATION_FAILED via the middleware), then stores the class with a trimmed name.</summary>
    public async Task<ClassDto> Handle(CreateClassCommand command, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);

        var created = await classes.AddAsync(
            new SchoolClass { Id = Guid.NewGuid(), Name = command.Name!.Trim(), TotalSeats = command.TotalSeats!.Value },
            cancellationToken);

        return ClassDto.WithoutHolds(created);
    }
}
