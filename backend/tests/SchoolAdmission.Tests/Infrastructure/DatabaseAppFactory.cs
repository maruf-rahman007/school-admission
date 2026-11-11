using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SchoolAdmission.Tests.Infrastructure;

/// <summary>
/// The real app, startup migrations included, running against <c>admission_test</c>.
/// Use it only from test classes in the <see cref="DatabaseCollection"/>, so the fixture has recreated the
/// database first and the tests in it run one after another (no concurrent migrations).
/// The expiry sweeper is effectively off (1-hour interval) unless <paramref name="settings"/> overrides
/// <c>EXPIRY_SWEEP_INTERVAL_SECONDS</c>, so tests can assert what query-time expiry alone does with stored rows.
/// </summary>
public sealed class DatabaseAppFactory(DatabaseFixture database, IReadOnlyDictionary<string, string?>? settings = null)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // The fixture's connection string has already passed the admission_test guard (TestDatabaseSettings).
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = database.ConnectionString,
                ["EXPIRY_SWEEP_INTERVAL_SECONDS"] = "3600",
            };
            foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            {
                values[key] = value;
            }

            config.AddInMemoryCollection(values);
        });
}
