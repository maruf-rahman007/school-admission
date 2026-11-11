using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Tests.Infrastructure;

/// <summary>
/// In-memory app host for tests. Removes the startup migration step, so these tests never touch a database,
/// whatever environment a test switches to.
/// </summary>
public class TestAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            var migrator = services.Single(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(DatabaseMigrator));
            services.Remove(migrator);
        });
}
