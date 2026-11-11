using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Application.Classes.Commands.CreateClass;
using SchoolAdmission.Application.Classes.Queries.GetClassById;
using SchoolAdmission.Application.Classes.Queries.GetClasses;

namespace SchoolAdmission.Application;

public static class DependencyInjection
{
    /// <summary>Registers the use-case handlers and their validators (plain classes, no mediator).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateClassCommand>, CreateClassValidator>();
        services.AddScoped<CreateClassHandler>();
        services.AddScoped<GetClassesHandler>();
        services.AddScoped<GetClassByIdHandler>();
        return services;
    }
}
