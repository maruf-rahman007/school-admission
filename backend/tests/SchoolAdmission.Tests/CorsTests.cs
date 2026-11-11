using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests;

public sealed class TwoOriginsFactory : TestAppFactory
{
    public const string FirstOrigin = "http://localhost:3000";
    public const string SecondOrigin = "http://127.0.0.1:3000";

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["CORS_ALLOWED_ORIGINS"] = $"{FirstOrigin},{SecondOrigin}" }));
    }
}

public class CorsTests(TwoOriginsFactory factory) : IClassFixture<TwoOriginsFactory>
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Preflight_FromAllowedOrigin_Returns204WithAllowOriginHeader()
    {
        var response = await SendPreflight(TwoOriginsFactory.FirstOrigin, "GET");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(TwoOriginsFactory.FirstOrigin, Single(response, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_ForDeleteWithStudentIdHeader_AllowsMethodAndHeader()
    {
        var response = await SendPreflight(TwoOriginsFactory.FirstOrigin, "DELETE", "X-Student-Id, Content-Type");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains("DELETE", Single(response, "Access-Control-Allow-Methods"));
        var allowedHeaders = Single(response, "Access-Control-Allow-Headers");
        Assert.Contains("X-Student-Id", allowedHeaders, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Content-Type", allowedHeaders, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("http://evil.test")]
    [InlineData("null")]
    public async Task Preflight_FromDisallowedOrigin_OmitsAllowOriginHeader(string origin)
    {
        var response = await SendPreflight(origin, "GET");

        Assert.True((int)response.StatusCode < 500);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Get_FromAllowedOrigin_ExposesServerTimeHeader()
    {
        var response = await GetHealth(TwoOriginsFactory.FirstOrigin);

        Assert.Equal(TwoOriginsFactory.FirstOrigin, Single(response, "Access-Control-Allow-Origin"));
        Assert.Contains(ServerTimeMiddleware.HeaderName, Single(response, "Access-Control-Expose-Headers"));
    }

    [Fact]
    public async Task Get_FromAllowedOrigin_DoesNotAllowCredentials()
    {
        var response = await GetHealth(TwoOriginsFactory.FirstOrigin);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Get_WithoutOriginHeader_Returns200WithoutCorsHeaders()
    {
        var response = await GetHealth(origin: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(response.Headers, h => h.Key.StartsWith("Access-Control-", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Preflight_FromSecondConfiguredOrigin_IsAllowed()
    {
        var response = await SendPreflight(TwoOriginsFactory.SecondOrigin, "POST");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(TwoOriginsFactory.SecondOrigin, Single(response, "Access-Control-Allow-Origin"));
    }

    private async Task<HttpResponseMessage> SendPreflight(string origin, string method, string? headers = null)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        if (headers is not null)
        {
            request.Headers.Add("Access-Control-Request-Headers", headers);
        }

        return await client.SendAsync(request, Ct);
    }

    private async Task<HttpResponseMessage> GetHealth(string? origin)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        return await client.SendAsync(request, Ct);
    }

    private static string Single(HttpResponseMessage response, string header) =>
        Assert.Single(response.Headers.GetValues(header));
}
