using System.Net;
using System.Text.Json;
using SchoolAdmission.Tests.Classes;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Holds;

/// <summary>
/// Expiry in real time through the real API (ADR-0002: no fake clock). Holds last 2 s; the sweeper is effectively off
/// (DatabaseAppFactory default), so every EXPIRED seen here comes from query-time expiry on the database clock.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class HoldExpiryTests(DatabaseFixture database) : IAsyncDisposable
{
    private const int HoldSeconds = 2;

    // Hold duration plus a safe margin, so no assert depends on the exact expires_at boundary.
    private static readonly TimeSpan PastExpiry = TimeSpan.FromSeconds(HoldSeconds + 1.5);

    private static readonly Dictionary<string, string?> ShortHolds = new() { ["HOLD_DURATION_SECONDS"] = $"{HoldSeconds}" };

    private readonly DatabaseAppFactory _factory = new(database, ShortHolds);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetHoldById_AfterHoldDuration_ReturnsExpired()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client, totalSeats: 1);
        var created = await ApplyAsync(client, classId, HoldApi.NewStudentId());
        var holdId = created.GetProperty("holdId").GetString()!;

        var before = await GetJsonAsync(client, $"/holds/{holdId}");
        Assert.Equal("HELD", before.GetProperty("status").GetString());

        await Task.Delay(PastExpiry, Ct);
        var after = await GetJsonAsync(client, $"/holds/{holdId}");

        Assert.Equal("EXPIRED", after.GetProperty("status").GetString());
        Assert.Equal(holdId, after.GetProperty("holdId").GetString());
        Assert.Equal(classId, after.GetProperty("classId").GetString());
        Assert.Equal(created.GetProperty("expiresAt").GetString(), after.GetProperty("expiresAt").GetString());
        // Query-time expiry alone: the sweeper has not touched the stored row.
        Assert.Equal("HELD", await HoldApi.StoredStatusAsync(database, HoldApi.GuidOf(holdId)));
    }

    [Fact]
    public async Task GetClassById_AfterHoldExpires_SeatAvailableAgain()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client, totalSeats: 1);
        await ApplyAsync(client, classId, HoldApi.NewStudentId());

        var full = await GetJsonAsync(client, $"/classes/{classId}");
        Assert.Equal(1, full.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, full.GetProperty("availableSeats").GetInt32());

        await Task.Delay(PastExpiry, Ct);
        var freed = await GetJsonAsync(client, $"/classes/{classId}");

        Assert.Equal(0, freed.GetProperty("heldSeats").GetInt32());
        Assert.Equal(1, freed.GetProperty("availableSeats").GetInt32());
        var other = await HoldApi.ApplyAsync(client, classId, HoldApi.NewStudentId());
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
    }

    [Fact]
    public async Task GetHoldById_AfterAppRestartAndExpiry_ReturnsExpiredAndSeatFree()
    {
        string classId;
        string holdId;
        await using (var first = new DatabaseAppFactory(database, ShortHolds))
        {
            var client = first.CreateClient();
            classId = await NewClassAsync(client, totalSeats: 1);
            holdId = (await ApplyAsync(client, classId, HoldApi.NewStudentId())).GetProperty("holdId").GetString()!;
        }

        await Task.Delay(PastExpiry, Ct);

        await using var second = new DatabaseAppFactory(database, ShortHolds);
        var restarted = second.CreateClient();
        var hold = await GetJsonAsync(restarted, $"/holds/{holdId}");
        var @class = await GetJsonAsync(restarted, $"/classes/{classId}");

        Assert.Equal("EXPIRED", hold.GetProperty("status").GetString());
        Assert.Equal(0, @class.GetProperty("heldSeats").GetInt32());
        Assert.Equal(1, @class.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task CreateHold_SameStudentAfterExpiry_Returns201()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client, totalSeats: 1);
        var studentId = HoldApi.NewStudentId();
        var oldHoldId = (await ApplyAsync(client, classId, studentId)).GetProperty("holdId").GetString()!;

        await Task.Delay(PastExpiry, Ct);
        var again = await HoldApi.ApplyAsync(client, classId, studentId);

        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        var body = await TestHost.ReadJsonAsync(again);
        Assert.NotEqual(oldHoldId, body.GetProperty("holdId").GetString());
        Assert.Equal("HELD", body.GetProperty("status").GetString());
        Assert.Equal("EXPIRED", (await GetJsonAsync(client, $"/holds/{oldHoldId}")).GetProperty("status").GetString());
        // The create flow's first step stored the student's stale hold as EXPIRED.
        Assert.Equal("EXPIRED", await HoldApi.StoredStatusAsync(database, HoldApi.GuidOf(oldHoldId)));
    }

    public async ValueTask DisposeAsync() => await _factory.DisposeAsync();

    private static async Task<string> NewClassAsync(HttpClient client, int totalSeats)
    {
        var created = await ClassApi.CreateAsync(client, $"{ClassApi.NewPrefix()} Expiry", totalSeats);
        return created.GetProperty("id").GetString()!;
    }

    private static async Task<JsonElement> ApplyAsync(HttpClient client, string classId, string studentId)
    {
        var response = await HoldApi.ApplyAsync(client, classId, studentId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await TestHost.ReadJsonAsync(response);
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await TestHost.ReadJsonAsync(response);
    }
}
