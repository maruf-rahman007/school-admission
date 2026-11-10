using Npgsql;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests;

// Pure guard tests: no database and no environment variables needed.
public class TestDatabaseSettingsTests
{
    [Theory]
    [InlineData("admission")]
    [InlineData("postgres")]
    [InlineData("Admission_Test")]
    public void Validate_WhenDatabaseIsNotAdmissionTest_Throws(string databaseName)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            TestDatabaseSettings.Validate($"Host=db;Database={databaseName};Username=u;Password=p"));

        Assert.Contains($"'{databaseName}'", ex.Message);
        Assert.Contains("'admission_test'", ex.Message);
    }

    [Fact]
    public void Validate_WhenDatabaseKeyMissing_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            TestDatabaseSettings.Validate("Host=db;Username=admission_test;Password=p"));

        Assert.Contains("'admission_test'", ex.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenValueMissingOrBlank_ThrowsWithVariableName(string? value)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TestDatabaseSettings.Validate(value));

        Assert.Contains("TEST_DB_CONNECTION", ex.Message);
        Assert.Contains("admission_test", ex.Message);
    }

    [Fact]
    public void Validate_WhenDatabaseIsAdmissionTest_ReturnsConnectionString()
    {
        var result = TestDatabaseSettings.Validate(
            "Host=db;Port=5432;Database=admission_test;Username=admission;Password=admission");

        var builder = new NpgsqlConnectionStringBuilder(result);
        Assert.Equal("admission_test", builder.Database);
        Assert.Equal("db", builder.Host);
    }
}
