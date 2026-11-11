using Npgsql;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests;

[Collection(DatabaseCollection.Name)]
public class TestDatabaseTests(DatabaseFixture database)
{
    [Fact]
    public async Task CurrentDatabase_WhenFixtureInitialized_ReturnsAdmissionTest()
    {
        var result = await ScalarAsync("SELECT current_database()");

        Assert.Equal("admission_test", result);
    }

    private async Task<object?> ScalarAsync(string sql)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync(cancellationToken);
    }
}
