using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Application.Classes.Commands.CreateClass;

namespace SchoolAdmission.Application;

public static class DependencyInjection
{
    /// <summary>Registers the use-case handlers and their validators (plain classes, no mediator).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateClassCommand>, CreateClassValidator>();
        services.AddScoped<CreateClassHandler>();
        return services;
    }
}
