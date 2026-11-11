using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using SchoolAdmission.Api.Filters;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Filters;

public class RequireStudentIdTests(TestAppFactory factory)
    : IClassFixture<TestAppFactory>
{
    private const string ValidId = "20190123456789012";

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RequireStudentId_WithValidId_PassesIdToAction()
    {
        var response = await GetAsync("/test-only/student", ValidId);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ValidId, (await TestHost.ReadJsonAsync(response)).GetProperty("studentId").GetString());
    }

    [Fact]
    public async Task RequireStudentId_WhenHeaderMissing_Returns400InvalidStudentId()
    {
        await AssertInvalidAsync(await GetAsync("/test-only/student"));
    }

    [Theory]
    [InlineData("2019012345678901")]
    [InlineData("201901234567890123")]
    [InlineData("")]
    [InlineData("2019012345678901a")]
    [InlineData("+2019012345678901")]
    [InlineData("-2019012345678901")]
    [InlineData("20190123 45678901")]
    [InlineData(" 20190123456789012")]
    [InlineData("20190123456789012 ")]
    public async Task RequireStudentId_WhenNotSeventeenDigits_Returns400InvalidStudentId(string value)
    {
        await AssertInvalidAsync(await GetAsync("/test-only/student", value));
    }

    [Fact]
    public async Task RequireStudentId_WithBanglaDigits_Returns400InvalidStudentId()
    {
        // Bangla digits are Unicode digits: an ASCII-only check must reject them.
        using var client = factory.WithTestControllers().CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test-only/student");
        request.Headers.TryAddWithoutValidation(RequireStudentIdAttribute.HeaderName, "২০১৯০১২৩৪৫৬৭৮৯০১২");

        await AssertInvalidAsync(await client.SendAsync(request, Ct));
    }

    [Fact]
    public async Task RequireStudentId_WithLeadingZeros_KeepsThemUnchanged()
    {
        var response = await GetAsync("/test-only/student", "00000000000000001");

        Assert.Equal("00000000000000001", (await TestHost.ReadJsonAsync(response)).GetProperty("studentId").GetString());
    }

    [Fact]
    public async Task RequireStudentId_WhenHeaderRepeated_Returns400InvalidStudentId()
    {
        using var client = factory.WithTestControllers().CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test-only/student");
        request.Headers.Add(RequireStudentIdAttribute.HeaderName, [ValidId, ValidId]);

        await AssertInvalidAsync(await client.SendAsync(request, Ct));
    }

    [Fact]
    public async Task RequireStudentId_WithInvalidHeaderAndUnparseableRouteId_Returns400()
    {
        await AssertInvalidAsync(await GetAsync("/test-only/student/not-an-id", "abc"));
    }

    [Fact]
    public async Task ActionWithoutAttribute_WithoutHeader_Returns200()
    {
        var response = await GetAsync("/test-only/student/open");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpResponseMessage> GetAsync(string path, string? studentId = null)
    {
        using var client = factory.WithTestControllers().CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (studentId is not null)
        {
            request.Headers.TryAddWithoutValidation(RequireStudentIdAttribute.HeaderName, studentId);
        }

        return await client.SendAsync(request, Ct);
    }

    private static async Task AssertInvalidAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal("INVALID_STUDENT_ID", body.GetProperty("userResponse").GetProperty("code").GetString());
        Assert.Equal("AppException", body.GetProperty("devResponse").GetProperty("exceptionType").GetString());
    }
}

[ApiController]
[Route("test-only/student")]
public class StudentIdTestController : ControllerBase
{
    [HttpGet]
    [RequireStudentId]
    public IActionResult Get() => Ok(new { studentId = HttpContext.GetStudentId() });

    [HttpGet("{holdId}")]
    [RequireStudentId]
    public IActionResult GetById(string holdId) =>
        holdId.StartsWith("h_", StringComparison.Ordinal) ? Ok() : NotFound();

    [HttpGet("open")]
    public IActionResult Open() => Ok();
}
