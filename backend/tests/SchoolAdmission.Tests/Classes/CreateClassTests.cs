using System.Net;
using System.Text;
using System.Text.Json;
using Npgsql;
using SchoolAdmission.Api.Ids;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Classes;

[Collection(DatabaseCollection.Name)]
public sealed class CreateClassTests(DatabaseFixture database) : IAsyncDisposable
{
    private readonly DatabaseAppFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string UniqueName(string label) => $"{label} {Guid.NewGuid():N}";

    private async Task<HttpResponseMessage> PostAsync(string json, Action<HttpRequestMessage>? configure = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/classes")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        configure?.Invoke(request);
        return await _factory.CreateClient().SendAsync(request, Ct);
    }

    private static string Body(string name, int seats) =>
        JsonSerializer.Serialize(new { name, totalSeats = seats });

    private static async Task AssertErrorAsync(HttpResponseMessage response, string code)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal(code, body.GetProperty("userResponse").GetProperty("code").GetString());
    }

    private static async Task AssertFieldErrorsAsync(HttpResponseMessage response, params (string Field, string Code)[] expected)
    {
        await AssertErrorAsync(response, "VALIDATION_FAILED");
        var user = (await TestHost.ReadJsonAsync(response)).GetProperty("userResponse");
        Assert.Equal(JsonValueKind.Null, user.GetProperty("resourceId").ValueKind);
        var actual = user.GetProperty("errors").EnumerateArray()
            .Select(e => (e.GetProperty("field").GetString()!, e.GetProperty("code").GetString()!))
            .ToList();
        Assert.Equal(expected.OrderBy(e => e.Field), actual.OrderBy(e => e.Item1));
    }

    [Fact]
    public async Task CreateClass_ValidRequest_Returns201WithFullClassShape()
    {
        var name = UniqueName("Class 6 – Morning");

        var response = await PostAsync(Body(name, 120));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal(
            ["id", "name", "totalSeats", "heldSeats", "confirmedSeats", "availableSeats", "serverTime"],
            body.EnumerateObject().Select(p => p.Name));
        Assert.Matches("^c_[0-9a-f]{32}$", body.GetProperty("id").GetString());
        Assert.Equal(name, body.GetProperty("name").GetString());
        Assert.Equal(120, body.GetProperty("totalSeats").GetInt32());
        Assert.Equal(0, body.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, body.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(120, body.GetProperty("availableSeats").GetInt32());
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$", body.GetProperty("serverTime").GetString());
        Assert.True(response.Headers.Contains("X-Server-Time"));
    }

    [Fact]
    public async Task CreateClass_ValidRequest_SetsLocationHeaderToClassUrl()
    {
        var response = await PostAsync(Body(UniqueName("Located"), 10));

        var id = (await TestHost.ReadJsonAsync(response)).GetProperty("id").GetString();
        Assert.Equal($"/classes/{id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreateClass_NameWithSurroundingSpaces_StoresAndReturnsTrimmedName()
    {
        var name = UniqueName("Class 6 –  Morning");

        var response = await PostAsync(Body($"  {name}\t ", 30));

        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal(name, body.GetProperty("name").GetString());
        Assert.True(ClassId.TryParse(body.GetProperty("id").GetString(), out var id));
        Assert.Equal(name, await StoredNameAsync(id));
    }

    [Theory]
    [InlineData("""{ "totalSeats": 10 }""")]
    [InlineData("""{ "name": null, "totalSeats": 10 }""")]
    [InlineData("""{ "name": "", "totalSeats": 10 }""")]
    [InlineData("""{ "name": "   ", "totalSeats": 10 }""")]
    [InlineData("""{ "name": " \t", "totalSeats": 10 }""")]
    public async Task CreateClass_NameMissingNullEmptyOrBlank_Returns400Required(string json) =>
        await AssertFieldErrorsAsync(await PostAsync(json), ("name", "REQUIRED"));

    [Theory]
    [InlineData("""{ "name": "Seatless" }""")]
    [InlineData("""{ "name": "Seatless", "totalSeats": null }""")]
    public async Task CreateClass_TotalSeatsMissingOrNull_Returns400Required(string json) =>
        await AssertFieldErrorsAsync(await PostAsync(json), ("totalSeats", "REQUIRED"));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task CreateClass_TotalSeatsZeroOrNegative_Returns400OutOfRange(int seats) =>
        await AssertFieldErrorsAsync(await PostAsync(Body("Bad seats", seats)), ("totalSeats", "OUT_OF_RANGE"));

    [Theory]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public async Task CreateClass_TotalSeatsOneOrIntMax_Returns201(int seats)
    {
        var response = await PostAsync(Body(UniqueName("Edge"), seats));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(seats, (await TestHost.ReadJsonAsync(response)).GetProperty("availableSeats").GetInt32());
    }

    [Theory]
    [InlineData("""{ "name": "Huge", "totalSeats": 2147483648 }""")]
    [InlineData("""{ "name": "Huge", "totalSeats": -2147483649 }""")]
    public async Task CreateClass_TotalSeatsBeyondIntRange_Returns400MalformedRequest(string json) =>
        await AssertErrorAsync(await PostAsync(json), "MALFORMED_REQUEST");

    [Theory]
    [InlineData("""{ "name": "Typed", "totalSeats": "abc" }""")]
    [InlineData("""{ "name": "Typed", "totalSeats": "40" }""")]
    [InlineData("""{ "name": "Typed", "totalSeats": 1.5 }""")]
    [InlineData("""{ "name": "Typed", "totalSeats": true }""")]
    [InlineData("""{ "name": 123, "totalSeats": 10 }""")]
    [InlineData("""[{ "name": "Typed", "totalSeats": 10 }]""")]
    public async Task CreateClass_WrongJsonTypes_Returns400MalformedRequest(string json) =>
        await AssertErrorAsync(await PostAsync(json), "MALFORMED_REQUEST");

    [Fact]
    public async Task CreateClass_NameAndSeatsInvalid_ReturnsBothFieldErrors() =>
        await AssertFieldErrorsAsync(
            await PostAsync("""{ "name": " ", "totalSeats": 0 }"""), ("name", "REQUIRED"), ("totalSeats", "OUT_OF_RANGE"));

    [Fact]
    public async Task CreateClass_DuplicateName_Returns201ForBoth()
    {
        var name = UniqueName("Twin");

        var first = await PostAsync(Body(name, 5));
        var parallel = await Task.WhenAll(PostAsync(Body(name, 5)), PostAsync(Body(name, 5)));

        HttpResponseMessage[] all = [first, .. parallel];
        Assert.All(all, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));
        var ids = new List<string?>();
        foreach (var response in all)
        {
            ids.Add((await TestHost.ReadJsonAsync(response)).GetProperty("id").GetString());
        }

        Assert.Equal(3, ids.Distinct().Count());
    }

    [Fact]
    public async Task CreateClass_BanglaName_RoundTripsUnchanged()
    {
        var name = UniqueName("ষষ্ঠ শ্রেণি – প্রভাতী");

        var response = await PostAsync(Body(name, 40));

        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal(name, body.GetProperty("name").GetString());
        Assert.True(ClassId.TryParse(body.GetProperty("id").GetString(), out var id));
        Assert.Equal(name, await StoredNameAsync(id));
    }

    [Fact]
    public async Task CreateClass_InvalidStudentIdHeader_StillReturns201()
    {
        var response = await PostAsync(Body(UniqueName("No student"), 10), r => r.Headers.Add("X-Student-Id", "abc"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateClass_UnknownExtraField_IsIgnored()
    {
        var json = $$"""{ "name": "{{UniqueName("Extra")}}", "totalSeats": 10, "heldSeats": 5, "colour": "red" }""";

        var response = await PostAsync(json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(0, (await TestHost.ReadJsonAsync(response)).GetProperty("heldSeats").GetInt32());
    }

    private async Task<string?> StoredNameAsync(Guid id)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand("SELECT name FROM classes WHERE id = @id", connection);
        command.Parameters.AddWithValue("id", id);
        return (string?)await command.ExecuteScalarAsync(Ct);
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
