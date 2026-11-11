using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Infrastructure.Persistence;
using SchoolAdmission.Infrastructure.Repositories;

namespace SchoolAdmission.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers persistence and the startup migration step. The connection string is resolved lazily from the validated settings,
    /// so configuration overrides (e.g. in tests) apply.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, Func<IServiceProvider, string> connectionString)
    {
        services.AddDbContext<AdmissionDbContext>((provider, options) => options.UseNpgsql(connectionString(provider)));
        services.AddHostedService<DatabaseMigrator>();
        services.AddScoped<IClassRepository, ClassRepository>();
        return services;
    }
}
