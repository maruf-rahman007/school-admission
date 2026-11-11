using System.Net;
using SchoolAdmission.Tests.Classes;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Holds;

[Collection(DatabaseCollection.Name)]
public sealed class GetHoldByIdTests(DatabaseFixture database) : IAsyncDisposable
{
    private const string IsoMillisZ = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$";

    private readonly DatabaseAppFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<(string ClassId, string Name)> NewClassAsync(HttpClient client)
    {
        var created = await ClassApi.CreateAsync(client, $"{ClassApi.NewPrefix()} Class 6 – Morning");
        return (created.GetProperty("id").GetString()!, created.GetProperty("name").GetString()!);
    }

    [Fact]
    public async Task GetHoldById_ExistingHeldHold_Returns200WithFullShapeClassNameAndServerTime()
    {
        var client = _factory.CreateClient();
        var (classId, name) = await NewClassAsync(client);
        var created = await TestHost.ReadJsonAsync(await HoldApi.ApplyAsync(client, classId, HoldApi.NewStudentId()));
        var holdId = created.GetProperty("holdId").GetString();

        var response = await client.GetAsync($"/holds/{holdId}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal(
            ["holdId", "classId", "status", "expiresAt", "className", "serverTime"],
            body.EnumerateObject().Select(p => p.Name));
        Assert.Equal(holdId, body.GetProperty("holdId").GetString());
        Assert.Equal(classId, body.GetProperty("classId").GetString());
        Assert.Equal("HELD", body.GetProperty("status").GetString());
        Assert.Equal(created.GetProperty("expiresAt").GetString(), body.GetProperty("expiresAt").GetString());
        Assert.Matches(IsoMillisZ, body.GetProperty("expiresAt").GetString());
        Assert.Equal(name, body.GetProperty("className").GetString());
        Assert.Matches(IsoMillisZ, body.GetProperty("serverTime").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-student")]
    public async Task GetHoldById_NoStudentIdHeader_Returns200(string? header)
    {
        var client = _factory.CreateClient();
        var (classId, _) = await NewClassAsync(client);
        var holdId = await HoldApi.SeedAsync(database, classId, "HELD", 600);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/holds/h_{holdId:N}");
        if (header is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Student-Id", header);
        }

        var response = await client.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHoldById_HeldHoldPastExpiry_ReturnsExpiredWithoutChangingRow()
    {
        var client = _factory.CreateClient();
        var (classId, _) = await NewClassAsync(client);
        var holdId = await HoldApi.SeedAsync(database, classId, "HELD", -1);
        var storedExpiresAt = await HoldApi.ScalarAsync<DateTime>(
            database, "SELECT expires_at FROM holds WHERE id = @id", ("id", holdId));

        var body = await TestHost.ReadJsonAsync(await client.GetAsync($"/holds/h_{holdId:N}", Ct));

        Assert.Equal("EXPIRED", body.GetProperty("status").GetString());
        Assert.Equal(
            storedExpiresAt.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture),
            body.GetProperty("expiresAt").GetString());
        // Reading never writes: the sweeper (T-140) marks it EXPIRED.
        Assert.Equal("HELD", await HoldApi.StoredStatusAsync(database, holdId));
    }

    [Theory]
    [InlineData("CONFIRMED", 600)]
    [InlineData("CONFIRMED", -600)]
    [InlineData("EXPIRED", -60)]
    [InlineData("CANCELLED", 600)]
    public async Task GetHoldById_StoredStatuses_ReturnedAsStored(string status, int expiresInSeconds)
    {
        var client = _factory.CreateClient();
        var (classId, _) = await NewClassAsync(client);
        var holdId = await HoldApi.SeedAsync(database, classId, status, expiresInSeconds);

        var body = await TestHost.ReadJsonAsync(await client.GetAsync($"/holds/h_{holdId:N}", Ct));

        Assert.Equal(status, body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetHoldById_UnknownId_Returns404HoldNotFoundWithResourceId()
    {
        var client = _factory.CreateClient();
        var unknown = $"h_{Guid.NewGuid():N}";

        var user = await HoldApi.AssertErrorAsync(
            await client.GetAsync($"/holds/{unknown}", Ct), HttpStatusCode.NotFound, "HOLD_NOT_FOUND");

        Assert.Equal(unknown, user.GetProperty("resourceId").GetString());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("h_3f2a9c1e-0b7d-4e5f-a6c8-d9e0f1a2b3c4")]
    [InlineData("c_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")]
    [InlineData("H_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")]
    [InlineData("h_৩f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")]
    public async Task GetHoldById_UnparseableId_Returns404HoldNotFound(string holdId)
    {
        var client = _factory.CreateClient();

        var user = await HoldApi.AssertErrorAsync(
            await client.GetAsync($"/holds/{Uri.EscapeDataString(holdId)}", Ct), HttpStatusCode.NotFound, "HOLD_NOT_FOUND");

        Assert.Equal(holdId, user.GetProperty("resourceId").GetString());
    }

    [Fact]
    public async Task GetHoldById_VeryLongId_Returns404HoldNotFound()
    {
        var client = _factory.CreateClient();

        await HoldApi.AssertErrorAsync(
            await client.GetAsync("/holds/h_" + new string('a', 5000), Ct), HttpStatusCode.NotFound, "HOLD_NOT_FOUND");
    }

    [Fact]
    public async Task GetHoldById_UppercaseHexId_Returns200WithLowercaseId()
    {
        var client = _factory.CreateClient();
        var (classId, _) = await NewClassAsync(client);
        var holdId = await HoldApi.SeedAsync(database, classId, "HELD", 600);

        var response = await client.GetAsync($"/holds/h_{holdId.ToString("N").ToUpperInvariant()}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"h_{holdId:N}", (await TestHost.ReadJsonAsync(response)).GetProperty("holdId").GetString());
    }

    [Fact]
    public async Task CreateHold_LocationHeader_PointsToGettableHold()
    {
        var client = _factory.CreateClient();
        var (classId, _) = await NewClassAsync(client);
        var created = await HoldApi.ApplyAsync(client, classId, HoldApi.NewStudentId());
        var posted = await TestHost.ReadJsonAsync(created);

        var body = await TestHost.ReadJsonAsync(await client.GetAsync(created.Headers.Location, Ct));

        Assert.Equal(posted.GetProperty("holdId").GetString(), body.GetProperty("holdId").GetString());
        Assert.Equal(posted.GetProperty("classId").GetString(), body.GetProperty("classId").GetString());
        Assert.Equal(posted.GetProperty("status").GetString(), body.GetProperty("status").GetString());
        Assert.Equal(posted.GetProperty("expiresAt").GetString(), body.GetProperty("expiresAt").GetString());
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
