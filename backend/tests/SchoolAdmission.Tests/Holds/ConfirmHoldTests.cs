using System.Net;
using System.Text.Json;
using SchoolAdmission.Tests.Classes;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Holds;

/// <summary>POST /holds/{holdId}/confirm (ARCHITECTURE §5.2; DECISIONS T-000 Q4, Q5, Q6, Q6b; T-150).</summary>
[Collection(DatabaseCollection.Name)]
public sealed class ConfirmHoldTests(DatabaseFixture database) : IAsyncDisposable
{
    private const string IsoMillisZ = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$";

    private readonly DatabaseAppFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ConfirmHold_OwnHeldHold_Returns200Confirmed()
    {
        var client = _factory.CreateClient();
        var (classId, holdId, student, created) = await ApplyAsync(client);

        var response = await HoldApi.ConfirmAsync(client, holdId, student);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal(
            ["holdId", "classId", "status", "expiresAt", "className", "serverTime"],
            body.EnumerateObject().Select(p => p.Name));
        Assert.Equal(holdId, body.GetProperty("holdId").GetString());
        Assert.Equal(classId, body.GetProperty("classId").GetString());
        Assert.Equal("CONFIRMED", body.GetProperty("status").GetString());
        Assert.Equal(created.GetProperty("expiresAt").GetString(), body.GetProperty("expiresAt").GetString());
        Assert.False(string.IsNullOrEmpty(body.GetProperty("className").GetString()));
        Assert.Matches(IsoMillisZ, body.GetProperty("serverTime").GetString());

        var id = HoldApi.GuidOf(holdId);
        Assert.Equal("CONFIRMED", await HoldApi.StoredStatusAsync(database, id));
        Assert.Equal(1, await HoldApi.CountAsync(database, "id = @id AND confirmed_at IS NOT NULL", ("id", id)));
    }

    [Fact]
    public async Task ConfirmHold_OwnHeldHold_MovesSeatFromHeldToConfirmed()
    {
        var client = _factory.CreateClient();
        var (classId, holdId, student, _) = await ApplyAsync(client, totalSeats: 3);

        Assert.Equal(HttpStatusCode.OK, (await HoldApi.ConfirmAsync(client, holdId, student)).StatusCode);

        var c = await GetJsonAsync(client, $"/classes/{classId}");
        Assert.Equal(0, c.GetProperty("heldSeats").GetInt32());
        Assert.Equal(1, c.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(2, c.GetProperty("availableSeats").GetInt32());
        Assert.Equal("CONFIRMED", (await GetJsonAsync(client, $"/holds/{holdId}")).GetProperty("status").GetString());
    }

    [Fact]
    public async Task ConfirmHold_AlreadyConfirmed_Returns200AndNoSideEffect()
    {
        var client = _factory.CreateClient();
        var (classId, holdId, student, _) = await ApplyAsync(client);
        Assert.Equal(HttpStatusCode.OK, (await HoldApi.ConfirmAsync(client, holdId, student)).StatusCode);
        var firstConfirmedAt = await ConfirmedAtAsync(holdId);

        var response = await HoldApi.ConfirmAsync(client, holdId, student);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal("CONFIRMED", body.GetProperty("status").GetString());
        Assert.Equal(holdId, body.GetProperty("holdId").GetString());
        Assert.Equal(firstConfirmedAt, await ConfirmedAtAsync(holdId));
        Assert.Equal(1, (await GetJsonAsync(client, $"/classes/{classId}")).GetProperty("confirmedSeats").GetInt32());
    }

    [Fact]
    public async Task ConfirmHold_OtherStudentsHold_Returns403NotHoldOwner()
    {
        var client = _factory.CreateClient();
        var (_, holdId, _, _) = await ApplyAsync(client);

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.ConfirmAsync(client, holdId, HoldApi.NewStudentId()), HttpStatusCode.Forbidden, "NOT_HOLD_OWNER");

        Assert.Equal(holdId, user.GetProperty("resourceId").GetString());
        Assert.Equal("HELD", await HoldApi.StoredStatusAsync(database, HoldApi.GuidOf(holdId)));
    }

    [Theory]
    [InlineData("HELD", -1)]
    [InlineData("EXPIRED", 600)]
    [InlineData("CANCELLED", 600)]
    [InlineData("CONFIRMED", 600)]
    public async Task ConfirmHold_OtherStudentsExpiredHold_Returns403AndWritesNothing(string status, int expiresInSeconds)
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var id = await HoldApi.SeedAsync(database, classId, status, expiresInSeconds);

        await HoldApi.AssertErrorAsync(
            await HoldApi.ConfirmAsync(client, HoldApi.HoldIdOf(id), HoldApi.NewStudentId()),
            HttpStatusCode.Forbidden, "NOT_HOLD_OWNER");

        Assert.Equal(status, await HoldApi.StoredStatusAsync(database, id));
    }

    [Fact]
    public async Task ConfirmHold_StoredExpiredHold_Returns410HoldExpired()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        var id = await HoldApi.SeedAsync(database, classId, "EXPIRED", -60, student);

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.ConfirmAsync(client, HoldApi.HoldIdOf(id), student), HttpStatusCode.Gone, "HOLD_EXPIRED");

        Assert.Equal(HoldApi.HoldIdOf(id), user.GetProperty("resourceId").GetString());
        Assert.Equal("EXPIRED", await HoldApi.StoredStatusAsync(database, id));
    }

    [Fact]
    public async Task ConfirmHold_HeldPastExpiry_Returns410AndStoresExpired()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        // Time is up but the sweeper has not marked it yet (it is off in DatabaseAppFactory).
        var id = await HoldApi.SeedAsync(database, classId, "HELD", -1, student);

        await HoldApi.AssertErrorAsync(
            await HoldApi.ConfirmAsync(client, HoldApi.HoldIdOf(id), student), HttpStatusCode.Gone, "HOLD_EXPIRED");

        // The EXPIRED write is committed, not rolled back with the error.
        Assert.Equal("EXPIRED", await HoldApi.StoredStatusAsync(database, id));
        Assert.Equal(1, await HoldApi.CountAsync(database, "id = @id AND confirmed_at IS NULL", ("id", id)));
    }

    [Fact]
    public async Task ConfirmHold_AfterHoldDuration_Returns410()
    {
        await using var shortHolds = new DatabaseAppFactory(
            database, new Dictionary<string, string?> { ["HOLD_DURATION_SECONDS"] = "2" });
        var client = shortHolds.CreateClient();
        var (_, holdId, student, _) = await ApplyAsync(client);

        await Task.Delay(TimeSpan.FromSeconds(3.5), Ct);

        await HoldApi.AssertErrorAsync(await HoldApi.ConfirmAsync(client, holdId, student), HttpStatusCode.Gone, "HOLD_EXPIRED");
        Assert.Equal("EXPIRED", await HoldApi.StoredStatusAsync(database, HoldApi.GuidOf(holdId)));
    }

    [Fact]
    public async Task ConfirmHold_CancelledHold_Returns409HoldCancelled()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        var id = await HoldApi.SeedAsync(database, classId, "CANCELLED", 600, student);

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.ConfirmAsync(client, HoldApi.HoldIdOf(id), student), HttpStatusCode.Conflict, "HOLD_CANCELLED");

        Assert.Equal(HoldApi.HoldIdOf(id), user.GetProperty("resourceId").GetString());
        Assert.Equal("CANCELLED", await HoldApi.StoredStatusAsync(database, id));
    }

    [Fact]
    public async Task ConfirmHold_ConfirmedHoldPastExpiresAt_Returns200Confirmed()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        var id = await HoldApi.SeedAsync(database, classId, "CONFIRMED", -60, student);

        var response = await HoldApi.ConfirmAsync(client, HoldApi.HoldIdOf(id), student);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("CONFIRMED", (await TestHost.ReadJsonAsync(response)).GetProperty("status").GetString());
        Assert.Equal("CONFIRMED", await HoldApi.StoredStatusAsync(database, id));
    }

    [Fact]
    public async Task ConfirmHold_MissingStudentIdHeader_Returns400InvalidStudentId()
    {
        var client = _factory.CreateClient();
        var (_, holdId, _, _) = await ApplyAsync(client);

        await HoldApi.AssertErrorAsync(
            await HoldApi.ConfirmAsync(client, holdId, null), HttpStatusCode.BadRequest, "INVALID_STUDENT_ID");

        Assert.Equal("HELD", await HoldApi.StoredStatusAsync(database, HoldApi.GuidOf(holdId)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567890123456")] // 16 digits
    [InlineData("123456789012345678")] // 18 digits
    [InlineData("1234567890123456a")]
    [InlineData(" 12345678901234567")]
    [InlineData("12345678901234567 ")]
    [InlineData("১২৩৪৫৬৭৮৯০১২৩৪৫৬৭")] // Bangla digits
    public async Task ConfirmHold_InvalidStudentIdHeader_Returns400InvalidStudentId(string studentId)
    {
        var client = _factory.CreateClient();
        var (_, holdId, _, _) = await ApplyAsync(client);

        await HoldApi.AssertErrorAsync(
            await HoldApi.ConfirmAsync(client, holdId, studentId), HttpStatusCode.BadRequest, "INVALID_STUDENT_ID");
    }

    [Theory]
    [InlineData("h_00000000000000000000000000000000")] // well-formed, unknown
    [InlineData("abc")] // unparseable
    public async Task ConfirmHold_InvalidHeaderAndUnknownHold_Returns400(string holdId)
    {
        var client = _factory.CreateClient();

        await HoldApi.AssertErrorAsync(
            await HoldApi.ConfirmAsync(client, holdId, "123"), HttpStatusCode.BadRequest, "INVALID_STUDENT_ID");
    }

    [Fact]
    public async Task ConfirmHold_UnknownHoldId_Returns404HoldNotFoundWithResourceId()
    {
        var client = _factory.CreateClient();
        var unknown = $"h_{Guid.NewGuid():N}";

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.ConfirmAsync(client, unknown, HoldApi.NewStudentId()), HttpStatusCode.NotFound, "HOLD_NOT_FOUND");

        Assert.Equal(unknown, user.GetProperty("resourceId").GetString());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("h_3f2a9c1e-0b7d-4e5f-a6c8-d9e0f1a2b3c4")]
    [InlineData("c_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")]
    [InlineData("H_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")]
    [InlineData("h_৩f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")]
    public async Task ConfirmHold_UnparseableHoldId_Returns404HoldNotFound(string holdId)
    {
        var client = _factory.CreateClient();

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.ConfirmAsync(client, Uri.EscapeDataString(holdId), HoldApi.NewStudentId()),
            HttpStatusCode.NotFound, "HOLD_NOT_FOUND");

        Assert.Equal(holdId, user.GetProperty("resourceId").GetString());
    }

    [Fact]
    public async Task ConfirmHold_VeryLongHoldId_Returns404HoldNotFound()
    {
        var client = _factory.CreateClient();

        await HoldApi.AssertErrorAsync(
            await HoldApi.ConfirmAsync(client, "h_" + new string('a', 5000), HoldApi.NewStudentId()),
            HttpStatusCode.NotFound, "HOLD_NOT_FOUND");
    }

    [Fact]
    public async Task ConfirmHold_UppercaseHexHoldId_Returns200WithLowercaseHoldId()
    {
        var client = _factory.CreateClient();
        var (_, holdId, student, _) = await ApplyAsync(client);

        var response = await HoldApi.ConfirmAsync(client, "h_" + holdId[2..].ToUpperInvariant(), student);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(holdId, (await TestHost.ReadJsonAsync(response)).GetProperty("holdId").GetString());
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static async Task<string> NewClassAsync(HttpClient client, int totalSeats = 10) =>
        (await ClassApi.CreateAsync(client, $"{ClassApi.NewPrefix()} Confirm", totalSeats)).GetProperty("id").GetString()!;

    /// <summary>A new class and a HELD hold in it for a fresh student, created through the API.</summary>
    private static async Task<(string ClassId, string HoldId, string StudentId, JsonElement Created)> ApplyAsync(
        HttpClient client, int totalSeats = 10)
    {
        var classId = await NewClassAsync(client, totalSeats);
        var student = HoldApi.NewStudentId();
        var response = await HoldApi.ApplyAsync(client, classId, student);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await TestHost.ReadJsonAsync(response);
        return (classId, created.GetProperty("holdId").GetString()!, student, created);
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await TestHost.ReadJsonAsync(response);
    }

    private Task<DateTime> ConfirmedAtAsync(string holdId) =>
        HoldApi.ScalarAsync<DateTime>(database, "SELECT confirmed_at FROM holds WHERE id = @id", ("id", HoldApi.GuidOf(holdId)));
}
