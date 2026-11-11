using System.Net;
using System.Text.Json;
using SchoolAdmission.Tests.Classes;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Holds;

/// <summary>DELETE /holds/{holdId} (ARCHITECTURE §5.3; DECISIONS T-000 Q1, Q3, Q4, Q5b; T-150).</summary>
[Collection(DatabaseCollection.Name)]
public sealed class CancelHoldTests(DatabaseFixture database) : IAsyncDisposable
{
    private readonly DatabaseAppFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CancelHold_OwnHeldHold_Returns204AndStoresCancelled()
    {
        var client = _factory.CreateClient();
        var (_, holdId, student) = await ApplyAsync(client);

        var response = await HoldApi.CancelAsync(client, holdId, student);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync(Ct));
        Assert.True(response.Headers.Contains("X-Server-Time"));
        var id = HoldApi.GuidOf(holdId);
        Assert.Equal("CANCELLED", await HoldApi.StoredStatusAsync(database, id));
        Assert.Equal(1, await HoldApi.CountAsync(database, "id = @id AND cancelled_at IS NOT NULL", ("id", id)));
        Assert.Equal("CANCELLED", (await GetJsonAsync(client, $"/holds/{holdId}")).GetProperty("status").GetString());
    }

    [Fact]
    public async Task CancelHold_OwnHeldHold_FreesSeat()
    {
        var client = _factory.CreateClient();
        var (classId, holdId, student) = await ApplyAsync(client, totalSeats: 1);
        Assert.Equal(0, (await GetJsonAsync(client, $"/classes/{classId}")).GetProperty("availableSeats").GetInt32());

        Assert.Equal(HttpStatusCode.NoContent, (await HoldApi.CancelAsync(client, holdId, student)).StatusCode);

        var c = await GetJsonAsync(client, $"/classes/{classId}");
        Assert.Equal(0, c.GetProperty("heldSeats").GetInt32());
        Assert.Equal(1, c.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task CancelHold_ThenApplyAgain_Returns201()
    {
        var client = _factory.CreateClient();
        var (classId, holdId, student) = await ApplyAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await HoldApi.CancelAsync(client, holdId, student)).StatusCode);

        var again = await HoldApi.ApplyAsync(client, classId, student);

        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        var newHoldId = (await TestHost.ReadJsonAsync(again)).GetProperty("holdId").GetString()!;
        Assert.NotEqual(holdId, newHoldId);

        // Cancelling the new hold leaves the old CANCELLED row untouched.
        var oldCancelledAt = await CancelledAtAsync(holdId);
        Assert.Equal(HttpStatusCode.NoContent, (await HoldApi.CancelAsync(client, newHoldId, student)).StatusCode);
        Assert.Equal(oldCancelledAt, await CancelledAtAsync(holdId));
        Assert.Equal("CANCELLED", await HoldApi.StoredStatusAsync(database, HoldApi.GuidOf(newHoldId)));
    }

    [Fact]
    public async Task CancelHold_AlreadyCancelled_Returns204Unchanged()
    {
        var client = _factory.CreateClient();
        var (_, holdId, student) = await ApplyAsync(client);
        Assert.Equal(HttpStatusCode.NoContent, (await HoldApi.CancelAsync(client, holdId, student)).StatusCode);
        var firstCancelledAt = await CancelledAtAsync(holdId);

        var response = await HoldApi.CancelAsync(client, holdId, student);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("CANCELLED", await HoldApi.StoredStatusAsync(database, HoldApi.GuidOf(holdId)));
        Assert.Equal(firstCancelledAt, await CancelledAtAsync(holdId));
    }

    [Fact]
    public async Task CancelHold_StoredExpiredHold_Returns204Unchanged()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        var id = await HoldApi.SeedAsync(database, classId, "EXPIRED", -60, student);

        Assert.Equal(HttpStatusCode.NoContent, (await HoldApi.CancelAsync(client, HoldApi.HoldIdOf(id), student)).StatusCode);

        Assert.Equal("EXPIRED", await HoldApi.StoredStatusAsync(database, id));
        Assert.Equal(1, await HoldApi.CountAsync(database, "id = @id AND cancelled_at IS NULL", ("id", id)));
    }

    [Fact]
    public async Task CancelHold_HeldPastExpiry_Returns204AndStoresExpired()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        var id = await HoldApi.SeedAsync(database, classId, "HELD", -1, student);

        Assert.Equal(HttpStatusCode.NoContent, (await HoldApi.CancelAsync(client, HoldApi.HoldIdOf(id), student)).StatusCode);

        Assert.Equal("EXPIRED", await HoldApi.StoredStatusAsync(database, id));
        Assert.Equal(1, await HoldApi.CountAsync(database, "id = @id AND cancelled_at IS NULL", ("id", id)));
    }

    [Fact]
    public async Task CancelHold_ConfirmedHold_Returns409HoldAlreadyConfirmed()
    {
        var client = _factory.CreateClient();
        var (classId, holdId, student) = await ApplyAsync(client);
        Assert.Equal(HttpStatusCode.OK, (await HoldApi.ConfirmAsync(client, holdId, student)).StatusCode);

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.CancelAsync(client, holdId, student), HttpStatusCode.Conflict, "HOLD_ALREADY_CONFIRMED");

        Assert.Equal(holdId, user.GetProperty("resourceId").GetString());
        Assert.Equal("CONFIRMED", await HoldApi.StoredStatusAsync(database, HoldApi.GuidOf(holdId)));
        Assert.Equal(1, (await GetJsonAsync(client, $"/classes/{classId}")).GetProperty("confirmedSeats").GetInt32());
    }

    [Fact]
    public async Task CancelHold_OtherStudentsHold_Returns403NotHoldOwner()
    {
        var client = _factory.CreateClient();
        var (_, holdId, _) = await ApplyAsync(client);

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.CancelAsync(client, holdId, HoldApi.NewStudentId()), HttpStatusCode.Forbidden, "NOT_HOLD_OWNER");

        Assert.Equal(holdId, user.GetProperty("resourceId").GetString());
        Assert.Equal("HELD", await HoldApi.StoredStatusAsync(database, HoldApi.GuidOf(holdId)));
    }

    [Theory]
    [InlineData("CONFIRMED", 600)]
    [InlineData("EXPIRED", -60)]
    [InlineData("CANCELLED", 600)]
    [InlineData("HELD", -1)]
    public async Task CancelHold_OtherStudentsConfirmedHold_Returns403(string status, int expiresInSeconds)
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var id = await HoldApi.SeedAsync(database, classId, status, expiresInSeconds);

        await HoldApi.AssertErrorAsync(
            await HoldApi.CancelAsync(client, HoldApi.HoldIdOf(id), HoldApi.NewStudentId()),
            HttpStatusCode.Forbidden, "NOT_HOLD_OWNER");

        Assert.Equal(status, await HoldApi.StoredStatusAsync(database, id));
    }

    [Fact]
    public async Task CancelHold_MissingStudentIdHeader_Returns400InvalidStudentId()
    {
        var client = _factory.CreateClient();
        var (_, holdId, _) = await ApplyAsync(client);

        await HoldApi.AssertErrorAsync(
            await HoldApi.CancelAsync(client, holdId, null), HttpStatusCode.BadRequest, "INVALID_STUDENT_ID");

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
    public async Task CancelHold_InvalidStudentIdHeader_Returns400InvalidStudentId(string studentId)
    {
        var client = _factory.CreateClient();
        var (_, holdId, _) = await ApplyAsync(client);

        await HoldApi.AssertErrorAsync(
            await HoldApi.CancelAsync(client, holdId, studentId), HttpStatusCode.BadRequest, "INVALID_STUDENT_ID");
    }

    [Fact]
    public async Task CancelHold_InvalidHeaderAndUnknownHold_Returns400()
    {
        var client = _factory.CreateClient();

        await HoldApi.AssertErrorAsync(
            await HoldApi.CancelAsync(client, "abc", "123"), HttpStatusCode.BadRequest, "INVALID_STUDENT_ID");
    }

    [Fact]
    public async Task CancelHold_UnknownHoldId_Returns404HoldNotFoundWithResourceId()
    {
        var client = _factory.CreateClient();
        var unknown = $"h_{Guid.NewGuid():N}";

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.CancelAsync(client, unknown, HoldApi.NewStudentId()), HttpStatusCode.NotFound, "HOLD_NOT_FOUND");

        Assert.Equal(unknown, user.GetProperty("resourceId").GetString());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("h_3f2a9c1e-0b7d-4e5f-a6c8-d9e0f1a2b3c4")]
    [InlineData("c_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")]
    [InlineData("H_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")]
    [InlineData("h_৩f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")]
    public async Task CancelHold_UnparseableHoldId_Returns404HoldNotFound(string holdId)
    {
        var client = _factory.CreateClient();

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.CancelAsync(client, Uri.EscapeDataString(holdId), HoldApi.NewStudentId()),
            HttpStatusCode.NotFound, "HOLD_NOT_FOUND");

        Assert.Equal(holdId, user.GetProperty("resourceId").GetString());
    }

    [Fact]
    public async Task CancelHold_PutOnHold_Returns405()
    {
        var client = _factory.CreateClient();
        var (_, holdId, _) = await ApplyAsync(client);

        var response = await client.PutAsync($"/holds/{holdId}", null, Ct);

        await HoldApi.AssertErrorAsync(response, HttpStatusCode.MethodNotAllowed, "METHOD_NOT_ALLOWED");
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static async Task<string> NewClassAsync(HttpClient client, int totalSeats = 10) =>
        (await ClassApi.CreateAsync(client, $"{ClassApi.NewPrefix()} Cancel", totalSeats)).GetProperty("id").GetString()!;

    /// <summary>A new class and a HELD hold in it for a fresh student, created through the API.</summary>
    private static async Task<(string ClassId, string HoldId, string StudentId)> ApplyAsync(HttpClient client, int totalSeats = 10)
    {
        var classId = await NewClassAsync(client, totalSeats);
        var student = HoldApi.NewStudentId();
        var response = await HoldApi.ApplyAsync(client, classId, student);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (classId, (await TestHost.ReadJsonAsync(response)).GetProperty("holdId").GetString()!, student);
    }

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string url)
    {
        var response = await client.GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await TestHost.ReadJsonAsync(response);
    }

    private Task<DateTime> CancelledAtAsync(string holdId) =>
        HoldApi.ScalarAsync<DateTime>(database, "SELECT cancelled_at FROM holds WHERE id = @id", ("id", HoldApi.GuidOf(holdId)));
}
