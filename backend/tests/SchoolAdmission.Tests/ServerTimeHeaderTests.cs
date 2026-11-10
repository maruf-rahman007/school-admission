using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SchoolAdmission.Api.Middleware;

namespace SchoolAdmission.Tests;

public class ServerTimeHeaderTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Format = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetHealth_Always_ReturnsSingleServerTimeHeaderInUtcIso8601()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health", Ct);

        var value = ServerTime(response);
        Assert.True(DateTime.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _));
        Assert.Equal(TimeSpan.Zero, DateTimeOffset.Parse(value, CultureInfo.InvariantCulture).Offset);
    }

    [Fact]
    public async Task GetHealth_ServerTimeHeader_IsWithinRequestWindow()
    {
        using var client = factory.CreateClient();

        var before = DateTimeOffset.UtcNow;
        var response = await client.GetAsync("/health", Ct);
        var after = DateTimeOffset.UtcNow;

        var serverTime = DateTimeOffset.Parse(ServerTime(response), CultureInfo.InvariantCulture);
        Assert.InRange(serverTime, before.AddSeconds(-2), after.AddSeconds(2));
    }

    [Fact]
    public async Task GetUnknownRoute_Returns404WithServerTimeHeader()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/nope", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        ServerTime(response);
    }

    [Fact]
    public async Task Preflight_FromAllowedOrigin_IncludesServerTimeHeader()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        ServerTime(response);
    }

    [Fact]
    public async Task BadRequestResponse_Always_IncludesServerTimeHeader()
    {
        // The controller below exists only in the test assembly; no production code returns 400 yet.
        using var withTestController = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
            services => services.AddControllers().AddApplicationPart(typeof(BadRequestTestController).Assembly)));
        using var client = withTestController.CreateClient();

        var response = await client.GetAsync("/test-only/bad-request", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ServerTime(response);
    }

    private static string ServerTime(HttpResponseMessage response) =>
        Assert.Single(response.Headers.GetValues(ServerTimeMiddleware.HeaderName));
}

[ApiController]
[Route("test-only/bad-request")]
public class BadRequestTestController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => BadRequest();
}
