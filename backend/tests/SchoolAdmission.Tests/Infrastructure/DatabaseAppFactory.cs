using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SchoolAdmission.Tests.Infrastructure;

/// <summary>
/// The real app, startup migrations included, running against <c>admission_test</c>.
/// Use it only from test classes in the <see cref="DatabaseCollection"/>, so the fixture has recreated the
/// database first and the tests in it run one after another (no concurrent migrations).
/// </summary>
public sealed class DatabaseAppFactory(DatabaseFixture database) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        // The fixture's connection string has already passed the admission_test guard (TestDatabaseSettings).
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Default"] = database.ConnectionString }));
}
