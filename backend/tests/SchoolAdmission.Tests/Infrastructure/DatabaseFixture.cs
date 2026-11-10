using Npgsql;

namespace SchoolAdmission.Tests.Infrastructure;

// Shared by every test in the "Database" collection: drops and recreates admission_test once per test run.
// The database is left in place afterwards so a failed run can be inspected; the next run recreates it.
public sealed class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        // Validate first: no SQL runs unless the target is admission_test.
        ConnectionString = TestDatabaseSettings.FromEnvironment();

        var maintenance = new NpgsqlConnectionStringBuilder(ConnectionString)
        {
            Database = "postgres",
            Pooling = false,
        };

        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new NpgsqlConnection(maintenance.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        // The name is a constant, never taken from the environment.
        await using (var drop = new NpgsqlCommand("DROP DATABASE IF EXISTS admission_test WITH (FORCE)", connection))
        {
            await drop.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var create = new NpgsqlCommand("CREATE DATABASE admission_test", connection))
        {
            await create.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
