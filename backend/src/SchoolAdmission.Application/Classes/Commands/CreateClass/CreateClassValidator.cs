using FluentValidation;
using SchoolAdmission.Application.Common.Errors;

namespace SchoolAdmission.Application.Classes.Commands.CreateClass;

/// <summary>DECISIONS Q9: the name is non-empty after trimming, totalSeats is an integer greater than 0. No other limits.</summary>
public sealed class CreateClassValidator : AbstractValidator<CreateClassCommand>
{
    public CreateClassValidator()
    {
        // IsNullOrWhiteSpace also treats Unicode spaces (e.g. U+00A0) as blank.
        RuleFor(c => c.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithErrorCode(FieldErrorCodes.Required)
            .WithMessage("Name is required.");

        RuleFor(c => c.TotalSeats)
            .NotNull()
            .WithErrorCode(FieldErrorCodes.Required)
            .WithMessage("Total seats is required.")
            .GreaterThan(0)
            .WithErrorCode(FieldErrorCodes.OutOfRange)
            .WithMessage("Total seats must be greater than 0.");
    }
}
