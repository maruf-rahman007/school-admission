using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests;

public class HealthEndpointTests(TestAppFactory factory)
    : IClassFixture<TestAppFactory>
{
    [Fact]
    public async Task GetHealth_WhenAppIsRunning_Returns200WithHealthyStatus()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("""{"status":"Healthy"}""", body);
    }

    [Fact]
    public async Task GetHealth_WhenAppIsRunning_ReturnsJsonContentType()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetHealth_WhenDatabaseUnreachable_StillReturns200()
    {
        using var unreachableDb = factory.WithWebHostBuilder(builder =>
            builder.UseSetting(
                "ConnectionStrings:Default",
                "Host=does-not-exist.invalid;Port=5432;Database=x;Username=x;Password=x;Timeout=1"));
        using var client = unreachableDb.CreateClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("""{"status":"Healthy"}""", body);
    }

    [Fact]
    public async Task GetUnknownRoute_WhenCalled_Returns404()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/nope", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
