using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SchoolAdmission.Api.Middleware;

namespace SchoolAdmission.Tests;

public class SwaggerTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("Production")]
    [InlineData("Development")]
    public async Task GetOpenApiDocument_InEachEnvironment_Returns200WithOpenApiJson(string environment)
    {
        using var client = InEnvironment(environment).CreateClient();

        var response = await client.GetAsync("/openapi/v1.json", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        Assert.StartsWith("3.", document.RootElement.GetProperty("openapi").GetString());
    }

    [Fact]
    public async Task GetSwaggerUi_InProduction_Returns200Html()
    {
        using var client = InEnvironment("Production").CreateClient();

        var page = await client.GetAsync("/swagger", Ct); // follows the redirect to /swagger/index.html
        var bundle = await client.GetAsync("/swagger/swagger-ui-bundle.js", Ct); // assets served locally

        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal("text/html", page.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, bundle.StatusCode);
    }

    [Fact]
    public async Task GetSwaggerUi_Always_PointsToOpenApiV1Document()
    {
        using var client = factory.CreateClient();

        var config = await client.GetStringAsync("/swagger/index.js", Ct);

        Assert.Contains("\"url\":\"/openapi/v1.json\"", config);
    }

    [Fact]
    public async Task GetOpenApiDocument_Always_IncludesServerTimeHeader()
    {
        using var client = factory.CreateClient();

        var document = await client.GetAsync("/openapi/v1.json", Ct);
        var swagger = await client.GetAsync("/swagger/index.html", Ct);

        Assert.Single(document.Headers.GetValues(ServerTimeMiddleware.HeaderName));
        Assert.Single(swagger.Headers.GetValues(ServerTimeMiddleware.HeaderName));
    }

    [Fact]
    public async Task GetOpenApiDocument_Always_ExcludesHealthEndpoint()
    {
        using var client = factory.CreateClient();

        using var document = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json", Ct));

        var paths = document.RootElement.TryGetProperty("paths", out var p)
            ? p.EnumerateObject().Select(path => path.Name).ToList()
            : [];
        Assert.DoesNotContain("/health", paths);
    }

    private WebApplicationFactory<Program> InEnvironment(string environment) =>
        factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
}
