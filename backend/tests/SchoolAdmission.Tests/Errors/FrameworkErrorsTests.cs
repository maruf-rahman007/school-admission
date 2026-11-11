using System.Net;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using SchoolAdmission.Api.Middleware;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Errors;

public class FrameworkErrorsTests(TestAppFactory factory)
    : IClassFixture<TestAppFactory>
{
    private const string BodyPath = "/test-only/body";

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("{\"name\": \"Class 6\", \"totalSeats\": 10")]
    [InlineData("{\"name\": \"Class 6\", \"totalSeats\": 10} trailing")]
    [InlineData("{'name': 'Class 6'}")]
    public async Task PostBody_WithMalformedJson_Returns400MalformedRequest(string json)
    {
        await AssertErrorAsync(await PostJsonAsync(json), HttpStatusCode.BadRequest, "MALFORMED_REQUEST");
    }

    [Theory]
    [InlineData("{\"name\": \"Class 6\", \"totalSeats\": \"abc\"}")]
    [InlineData("{\"name\": \"Class 6\", \"totalSeats\": 1.5}")]
    [InlineData("{\"name\": 123, \"totalSeats\": 10}")]
    [InlineData("[{\"name\": \"Class 6\", \"totalSeats\": 10}]")]
    public async Task PostBody_WithWrongPropertyType_Returns400MalformedRequest(string json)
    {
        await AssertErrorAsync(await PostJsonAsync(json), HttpStatusCode.BadRequest, "MALFORMED_REQUEST");
    }

    [Fact]
    public async Task PostBody_WithNumberOutsideIntRange_Returns400MalformedRequest()
    {
        var response = await PostJsonAsync("{\"name\": \"Class 6\", \"totalSeats\": 99999999999}");

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "MALFORMED_REQUEST");
    }

    [Fact]
    public async Task PostBody_WithEmptyBody_Returns400MalformedRequest()
    {
        await AssertErrorAsync(await PostJsonAsync(""), HttpStatusCode.BadRequest, "MALFORMED_REQUEST");
    }

    [Fact]
    public async Task PostBody_WithNullBody_Returns400MalformedRequest()
    {
        await AssertErrorAsync(await PostJsonAsync("null"), HttpStatusCode.BadRequest, "MALFORMED_REQUEST");
    }

    [Fact]
    public async Task PostBody_MissingRequiredField_Returns400ValidationFailed()
    {
        var response = await PostJsonAsync("{\"totalSeats\": 10}");

        var user = await AssertErrorAsync(response, HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        var error = Assert.Single(user.GetProperty("errors").EnumerateArray());
        Assert.Equal("name", error.GetProperty("field").GetString());
        Assert.Equal("REQUIRED", error.GetProperty("code").GetString());
    }

    [Fact]
    public async Task PostBody_FailingValidator_Returns400ValidationFailedWithFieldErrors()
    {
        var response = await PostJsonAsync("{\"name\": \"  \", \"totalSeats\": 0}");

        var user = await AssertErrorAsync(response, HttpStatusCode.BadRequest, "VALIDATION_FAILED");
        var errors = user.GetProperty("errors").EnumerateArray()
            .Select(e => (e.GetProperty("field").GetString(), e.GetProperty("code").GetString()))
            .ToList();
        Assert.Equal([("name", "REQUIRED"), ("totalSeats", "OUT_OF_RANGE")], errors);
    }

    [Fact]
    public async Task GetUnknownRoute_Returns404RouteNotFound()
    {
        using var client = factory.WithTestControllers().CreateClient();

        var response = await client.GetAsync("/nope", Ct);

        var body = await AssertErrorBodyAsync(response, HttpStatusCode.NotFound, "ROUTE_NOT_FOUND");
        Assert.Single(response.Headers.GetValues(ServerTimeMiddleware.HeaderName));
        var dev = body.GetProperty("devResponse");
        Assert.Equal(JsonValueKind.Null, dev.GetProperty("exceptionType").ValueKind);
        Assert.Equal(JsonValueKind.Null, dev.GetProperty("stackTrace").ValueKind);
        Assert.False(string.IsNullOrEmpty(dev.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task WrongMethod_OnKnownRoute_Returns405MethodNotAllowed()
    {
        using var client = factory.WithTestControllers().CreateClient();

        var response = await client.PutAsync("/test-only/no-body", null, Ct);

        await AssertErrorBodyAsync(response, HttpStatusCode.MethodNotAllowed, "METHOD_NOT_ALLOWED");
        Assert.Single(response.Headers.GetValues(ServerTimeMiddleware.HeaderName));
    }

    [Fact]
    public async Task PostBody_WithTextPlainContentType_Returns415UnsupportedMediaType()
    {
        var response = await PostAsync(new StringContent("{\"name\":\"x\",\"totalSeats\":1}", Encoding.UTF8, "text/plain"));

        await AssertErrorAsync(response, HttpStatusCode.UnsupportedMediaType, "UNSUPPORTED_MEDIA_TYPE");
    }

    [Fact]
    public async Task PostBody_WithoutContentType_Returns415UnsupportedMediaType()
    {
        await AssertErrorAsync(await PostAsync(null), HttpStatusCode.UnsupportedMediaType, "UNSUPPORTED_MEDIA_TYPE");
    }

    [Fact]
    public async Task PostBody_WithJsonCharsetContentType_IsAccepted()
    {
        var response = await PostAsync(
            new StringContent("{\"name\":\"Class 6\",\"totalSeats\":1}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("utf-8", response.RequestMessage!.Content!.Headers.ContentType!.CharSet);
    }

    [Fact]
    public async Task PostWithoutBody_OnActionWithoutBodyParameter_IsAccepted()
    {
        using var client = factory.WithTestControllers().CreateClient();

        var response = await client.PostAsync("/test-only/no-body", null, Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task BareNotFoundResult_IsWrappedInErrorBody()
    {
        using var client = factory.WithTestControllers().CreateClient();

        var response = await client.GetAsync("/test-only/bare-not-found", Ct);

        await AssertErrorBodyAsync(response, HttpStatusCode.NotFound, "ROUTE_NOT_FOUND");
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/swagger/index.html")]
    public async Task ExistingEndpoints_AfterErrorHandling_StillReturn200(string path)
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task<HttpResponseMessage> PostJsonAsync(string json) =>
        PostAsync(new StringContent(json, Encoding.UTF8, "application/json"));

    private async Task<HttpResponseMessage> PostAsync(HttpContent? content)
    {
        using var client = factory.WithTestControllers().CreateClient();
        return await client.PostAsync(BodyPath, content, Ct);
    }

    private static async Task<JsonElement> AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code) =>
        (await AssertErrorBodyAsync(response, status, code)).GetProperty("userResponse");

    private static async Task<JsonElement> AssertErrorBodyAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal(code, body.GetProperty("userResponse").GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Array, body.GetProperty("userResponse").GetProperty("errors").ValueKind);
        Assert.True(body.TryGetProperty("devResponse", out _));
        return body;
    }
}

public sealed class BodyTestRequest
{
    public string Name { get; init; } = null!;

    public int TotalSeats { get; init; }
}

public sealed class BodyTestRequestValidator : AbstractValidator<BodyTestRequest>
{
    public BodyTestRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("REQUIRED");
        RuleFor(x => x.TotalSeats).GreaterThan(0).WithErrorCode("OUT_OF_RANGE");
    }
}

[ApiController]
[Route("test-only")]
public class FrameworkErrorsTestController : ControllerBase
{
    [HttpPost("body")]
    public IActionResult Body([FromBody] BodyTestRequest request)
    {
        new BodyTestRequestValidator().ValidateAndThrow(request);
        return Ok(new { request.Name, request.TotalSeats });
    }

    [HttpPost("no-body")]
    public IActionResult NoBody() => NoContent();

    [HttpGet("bare-not-found")]
    public IActionResult BareNotFound() => NotFound();
}
