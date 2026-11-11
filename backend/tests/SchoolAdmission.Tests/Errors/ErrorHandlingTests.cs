using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Application.Common.Errors;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Errors;

public class ErrorHandlingTests(TestAppFactory factory)
    : IClassFixture<TestAppFactory>
{
    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AppException_WhenThrown_ReturnsItsStatusAndCode()
    {
        var response = await GetAsync("/test-only/errors/app");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var user = (await TestHost.ReadJsonAsync(response)).GetProperty("userResponse");
        Assert.Equal("NO_SEATS_AVAILABLE", user.GetProperty("code").GetString());
        Assert.Equal("No seats are available in this class.", user.GetProperty("message").GetString());
        Assert.Equal(JsonValueKind.Null, user.GetProperty("resourceId").ValueKind);
        Assert.Equal(0, user.GetProperty("errors").GetArrayLength());
    }

    [Fact]
    public async Task AppException_WithResourceId_ReturnsResourceIdInUserResponse()
    {
        var response = await GetAsync("/test-only/errors/app-with-resource");

        var user = (await TestHost.ReadJsonAsync(response)).GetProperty("userResponse");
        Assert.Equal("c_0123abc", user.GetProperty("resourceId").GetString());
    }

    [Fact]
    public async Task AppException_WithFieldErrors_ReturnsErrorsArray()
    {
        var response = await GetAsync("/test-only/errors/app-with-errors");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await TestHost.ReadJsonAsync(response))
            .GetProperty("userResponse").GetProperty("errors").EnumerateArray());
        Assert.Equal("totalSeats", error.GetProperty("field").GetString());
        Assert.Equal("OUT_OF_RANGE", error.GetProperty("code").GetString());
        Assert.Equal("Must be at least 1.", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task UnhandledException_WhenThrown_Returns500InternalError()
    {
        var response = await GetAsync("/test-only/errors/crash");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await TestHost.ReadJsonAsync(response);
        var user = body.GetProperty("userResponse");
        Assert.Equal("INTERNAL_ERROR", user.GetProperty("code").GetString());
        Assert.DoesNotContain("secret detail", user.GetProperty("message").GetString());
        Assert.Equal("secret detail", body.GetProperty("devResponse").GetProperty("message").GetString());
    }

    [Fact]
    public async Task UnhandledException_WhenThrown_IsLoggedWithTraceId()
    {
        var logs = new CapturingLoggerProvider();
        using var client = factory.WithTestControllers(logs).CreateClient();

        var response = await client.GetAsync("/test-only/errors/crash", Ct);

        var traceId = (await TestHost.ReadJsonAsync(response)).GetProperty("devResponse").GetProperty("traceId").GetString();
        Assert.False(string.IsNullOrEmpty(traceId));
        Assert.Contains(logs.Entries, e =>
            e.Level >= LogLevel.Error && e.Exception is InvalidOperationException && e.Message.Contains(traceId!));
    }

    [Fact]
    public async Task ErrorResponse_Always_IncludesDevResponseWithTraceIdAndStackTrace()
    {
        var response = await GetAsync("/test-only/errors/app");

        var dev = (await TestHost.ReadJsonAsync(response)).GetProperty("devResponse");
        Assert.Equal(nameof(AppException), dev.GetProperty("exceptionType").GetString());
        Assert.Equal("No seats are available in this class.", dev.GetProperty("message").GetString());
        Assert.False(string.IsNullOrEmpty(dev.GetProperty("traceId").GetString()));
        Assert.Contains(nameof(ErrorsTestController), dev.GetProperty("stackTrace").GetString());
    }

    [Theory]
    [InlineData("/test-only/errors/app")]
    [InlineData("/test-only/errors/crash")]
    [InlineData("/test-only/bad-request")]
    public async Task ErrorResponse_Always_IsApplicationJsonNotProblemJson(string path)
    {
        var response = await GetAsync(path);

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ErrorResponse_FromAllowedOrigin_IncludesCorsAndServerTimeHeaders()
    {
        using var client = factory.WithTestControllers().CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test-only/errors/crash");
        request.Headers.Add("Origin", "http://localhost:3000");

        var response = await client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("http://localhost:3000", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains(ServerTimeMiddleware.HeaderName, Assert.Single(response.Headers.GetValues("Access-Control-Expose-Headers")));
        Assert.Single(response.Headers.GetValues(ServerTimeMiddleware.HeaderName));
    }

    [Fact]
    public async Task AbortedRequest_WhenClientCancels_IsNotLoggedAsError()
    {
        var logs = new CapturingLoggerProvider();
        using var client = factory.WithTestControllers(logs).CreateClient();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        cancel.CancelAfter(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GetAsync("/test-only/errors/slow", cancel.Token));

        // Give the server side a moment to observe the abort and finish the request.
        await Task.Delay(500, Ct);
        Assert.DoesNotContain(logs.Entries, e => e.Level >= LogLevel.Error);
        Assert.Contains(logs.Entries, e => e.Message.StartsWith("Request aborted by the client", StringComparison.Ordinal));
    }

    private async Task<HttpResponseMessage> GetAsync(string path)
    {
        using var client = factory.WithTestControllers().CreateClient();
        return await client.GetAsync(path, Ct);
    }
}

[ApiController]
[Route("test-only/errors")]
public class ErrorsTestController : ControllerBase
{
    [HttpGet("app")]
    public IActionResult App() =>
        throw new AppException("NO_SEATS_AVAILABLE", 409, "No seats are available in this class.");

    [HttpGet("app-with-resource")]
    public IActionResult AppWithResource() =>
        throw new AppException("CLASS_NOT_FOUND", 404, "Class not found.", resourceId: "c_0123abc");

    [HttpGet("app-with-errors")]
    public IActionResult AppWithErrors() =>
        throw new AppException("VALIDATION_FAILED", 400, "Validation failed.",
            errors: [new FieldError("totalSeats", "OUT_OF_RANGE", "Must be at least 1.")]);

    [HttpGet("slow")]
    public async Task<IActionResult> Slow(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        return Ok();
    }

    [HttpGet("crash")]
    public IActionResult Crash() => throw new InvalidOperationException("secret detail");
}
