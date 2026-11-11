using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Application.Classes.Commands.CreateClass;
using SchoolAdmission.Application.Classes.Queries.GetClassById;
using SchoolAdmission.Application.Classes.Queries.GetClasses;
using SchoolAdmission.Application.Holds.Commands.CancelHold;
using SchoolAdmission.Application.Holds.Commands.ConfirmHold;
using SchoolAdmission.Application.Holds.Commands.CreateHold;
using SchoolAdmission.Application.Holds.Queries.GetHoldById;
using SchoolAdmission.Application.Holds.Queries.GetMyHolds;

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
        services.AddScoped<CreateHoldHandler>();
        services.AddScoped<GetHoldByIdHandler>();
        services.AddScoped<ConfirmHoldHandler>();
        services.AddScoped<CancelHoldHandler>();
        services.AddScoped<GetMyHoldsHandler>();
        return services;
    }
}
