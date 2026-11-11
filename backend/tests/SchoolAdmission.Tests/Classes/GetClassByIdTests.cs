using System.Net;
using System.Text.Json;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Classes;

[Collection(DatabaseCollection.Name)]
public sealed class GetClassByIdTests(DatabaseFixture database) : IAsyncDisposable
{
    private readonly DatabaseAppFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task AssertClassNotFoundAsync(HttpResponseMessage response, string requestedId)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await TestHost.ReadJsonAsync(response);
        var user = body.GetProperty("userResponse");
        Assert.Equal("CLASS_NOT_FOUND", user.GetProperty("code").GetString());
        Assert.Equal(requestedId, user.GetProperty("resourceId").GetString());
        Assert.Equal(0, user.GetProperty("errors").GetArrayLength());
        Assert.Equal(JsonValueKind.Object, body.GetProperty("devResponse").ValueKind);
    }

    [Fact]
    public async Task GetClassById_ExistingId_Returns200WithFullShapeAndServerTime()
    {
        var client = _factory.CreateClient();
        var created = await ClassApi.CreateAsync(client, $"{ClassApi.NewPrefix()} Class 6", 40);
        var id = created.GetProperty("id").GetString();

        var response = await client.GetAsync($"/classes/{id}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal(
            ["id", "name", "totalSeats", "heldSeats", "confirmedSeats", "availableSeats", "serverTime"],
            body.EnumerateObject().Select(p => p.Name));
        Assert.Equal(id, body.GetProperty("id").GetString());
        Assert.Equal(created.GetProperty("name").GetString(), body.GetProperty("name").GetString());
        Assert.Equal(40, body.GetProperty("totalSeats").GetInt32());
        Assert.Equal(0, body.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, body.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(40, body.GetProperty("availableSeats").GetInt32());
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$", body.GetProperty("serverTime").GetString());
    }

    [Fact]
    public async Task GetClassById_UppercaseHexId_Returns200WithLowercaseId()
    {
        var client = _factory.CreateClient();
        var id = (await ClassApi.CreateAsync(client, $"{ClassApi.NewPrefix()} Upper")).GetProperty("id").GetString()!;

        var response = await client.GetAsync($"/classes/c_{id[2..].ToUpperInvariant()}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(id, (await TestHost.ReadJsonAsync(response)).GetProperty("id").GetString());
    }

    [Fact]
    public async Task GetClassById_UnknownId_Returns404ClassNotFoundWithResourceId()
    {
        var unknown = $"c_{Guid.NewGuid():N}";

        var response = await _factory.CreateClient().GetAsync($"/classes/{unknown}", Ct);

        await AssertClassNotFoundAsync(response, unknown);
    }

    public static TheoryData<string> UnparseableIds() =>
    [
        "abc",
        "c_",
        "c_৩f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4",
        "c_3f2a9c1e-0b7d-4e5f-a6c8-d9e0f1a2b3c4",
        "C_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4",
        "h_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4",
        "c_ 3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c",
        "c_" + new string('a', 3000),
    ];

    [Theory]
    [MemberData(nameof(UnparseableIds))]
    public async Task GetClassById_UnparseableId_Returns404ClassNotFound(string id)
    {
        var response = await _factory.CreateClient().GetAsync($"/classes/{Uri.EscapeDataString(id)}", Ct);

        await AssertClassNotFoundAsync(response, id);
    }

    [Fact]
    public async Task CreateClass_LocationHeader_PointsToGettableClass()
    {
        var client = _factory.CreateClient();
        var create = await client.PostAsync(
            "/classes",
            new StringContent($$"""{ "name": "{{ClassApi.NewPrefix()}} Located", "totalSeats": 7 }""", System.Text.Encoding.UTF8, "application/json"),
            Ct);
        var created = await TestHost.ReadJsonAsync(create);

        var fetched = await TestHost.ReadJsonAsync(await client.GetAsync(create.Headers.Location, Ct));

        foreach (var field in new[] { "id", "name", "totalSeats", "heldSeats", "confirmedSeats", "availableSeats" })
        {
            Assert.Equal(created.GetProperty(field).ToString(), fetched.GetProperty(field).ToString());
        }
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
