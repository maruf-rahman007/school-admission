using Npgsql;

namespace SchoolAdmission.Tests.Infrastructure;

// Reads the test database connection from TEST_DB_CONNECTION and refuses anything that is not admission_test,
// because the database fixture drops and recreates that database.
public static class TestDatabaseSettings
{
    public const string VariableName = "TEST_DB_CONNECTION";
    public const string DatabaseName = "admission_test";

    public static string FromEnvironment() => Validate(Environment.GetEnvironmentVariable(VariableName));

    public static string Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{VariableName} is not set. It must point at the '{DatabaseName}' database. " +
                "Run the tests with: docker compose run --rm backend-tests");
        }

        NpgsqlConnectionStringBuilder builder;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(value);
        }
        catch (ArgumentException ex)
        {
            // The value itself is not echoed: it may contain a password.
            throw new InvalidOperationException(
                $"{VariableName} is not a valid PostgreSQL connection string. It must point at the '{DatabaseName}' database.",
                ex);
        }

        if (!string.Equals(builder.Database, DatabaseName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{VariableName} points at database '{builder.Database ?? "(none)"}', expected '{DatabaseName}'. " +
                "The test fixture drops and recreates this database, so it refuses any other name.");
        }

        return builder.ConnectionString;
    }
}
