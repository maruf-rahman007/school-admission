using System.Globalization;
using System.Net;
using SchoolAdmission.Tests.Classes;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Holds;

[Collection(DatabaseCollection.Name)]
public sealed class CreateHoldTests(DatabaseFixture database) : IAsyncDisposable
{
    private const string IsoMillisZ = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$";

    private readonly DatabaseAppFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<string> NewClassAsync(HttpClient client, int totalSeats = 10) =>
        (await ClassApi.CreateAsync(client, $"{ClassApi.NewPrefix()} Holds", totalSeats)).GetProperty("id").GetString()!;

    [Fact]
    public async Task CreateHold_ValidRequest_Returns201WithHeldHold()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();

        var response = await HoldApi.ApplyAsync(client, classId, student);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal(
            ["holdId", "classId", "studentId", "status", "expiresAt", "serverTime"],
            body.EnumerateObject().Select(p => p.Name));
        Assert.Matches("^h_[0-9a-f]{32}$", body.GetProperty("holdId").GetString());
        Assert.Equal(classId, body.GetProperty("classId").GetString());
        Assert.Equal(student, body.GetProperty("studentId").GetString());
        Assert.Equal("HELD", body.GetProperty("status").GetString());
        Assert.Matches(IsoMillisZ, body.GetProperty("expiresAt").GetString());
        Assert.Matches(IsoMillisZ, body.GetProperty("serverTime").GetString());

        var holdId = HoldApi.GuidOf(body.GetProperty("holdId").GetString()!);
        Assert.Equal("HELD", await HoldApi.StoredStatusAsync(database, holdId));
    }

    [Fact]
    public async Task CreateHold_ValidRequest_SetsLocationHeaderToHoldUrl()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);

        var response = await HoldApi.ApplyAsync(client, classId, HoldApi.NewStudentId());

        var holdId = (await TestHost.ReadJsonAsync(response)).GetProperty("holdId").GetString();
        Assert.Equal($"/holds/{holdId}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreateHold_ValidRequest_ExpiresAtIsHoldDurationAfterServerTime()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);

        var body = await TestHost.ReadJsonAsync(await HoldApi.ApplyAsync(client, classId, HoldApi.NewStudentId()));

        var expiresAt = DateTimeOffset.Parse(body.GetProperty("expiresAt").GetString()!, CultureInfo.InvariantCulture);
        var serverTime = DateTimeOffset.Parse(body.GetProperty("serverTime").GetString()!, CultureInfo.InvariantCulture);
        // HOLD_DURATION_SECONDS defaults to 600.
        Assert.InRange((expiresAt - serverTime).TotalSeconds, 595, 605);
    }

    [Fact]
    public async Task CreateHold_ValidRequest_IncreasesHeldSeatsOfClass()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client, 3);

        await HoldApi.ApplyAsync(client, classId, HoldApi.NewStudentId());

        var c = await TestHost.ReadJsonAsync(await client.GetAsync($"/classes/{classId}", Ct));
        Assert.Equal(1, c.GetProperty("heldSeats").GetInt32());
        Assert.Equal(2, c.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task CreateHold_MissingStudentIdHeader_Returns400InvalidStudentId()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);

        await HoldApi.AssertErrorAsync(
            await HoldApi.ApplyAsync(client, classId, null), HttpStatusCode.BadRequest, "INVALID_STUDENT_ID");
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567890123456")] // 16 digits
    [InlineData("123456789012345678")] // 18 digits
    [InlineData("1234567890123456a")]
    [InlineData(" 12345678901234567")]
    [InlineData("12345678901234567 ")]
    [InlineData("১২৩৪৫৬৭৮৯০১২৩৪৫৬৭")] // Bangla digits
    public async Task CreateHold_InvalidStudentIdHeader_Returns400InvalidStudentId(string studentId)
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);

        await HoldApi.AssertErrorAsync(
            await HoldApi.ApplyAsync(client, classId, studentId), HttpStatusCode.BadRequest, "INVALID_STUDENT_ID");
    }

    [Fact]
    public async Task CreateHold_UnknownClassId_Returns404ClassNotFoundWithResourceId()
    {
        var client = _factory.CreateClient();
        var unknown = $"c_{Guid.NewGuid():N}";

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.ApplyAsync(client, unknown, HoldApi.NewStudentId()), HttpStatusCode.NotFound, "CLASS_NOT_FOUND");

        Assert.Equal(unknown, user.GetProperty("resourceId").GetString());
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("c_3f2a9c1e-0b7d-4e5f-a6c8-d9e0f1a2b3c4")]
    [InlineData("h_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")]
    [InlineData("C_3f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")]
    [InlineData("c_৩f2a9c1e0b7d4e5fa6c8d9e0f1a2b3c4")]
    public async Task CreateHold_UnparseableClassId_Returns404ClassNotFound(string classId)
    {
        var client = _factory.CreateClient();

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.ApplyAsync(client, Uri.EscapeDataString(classId), HoldApi.NewStudentId()),
            HttpStatusCode.NotFound, "CLASS_NOT_FOUND");

        Assert.Equal(classId, user.GetProperty("resourceId").GetString());
    }

    [Fact]
    public async Task CreateHold_VeryLongClassId_Returns404ClassNotFound()
    {
        var client = _factory.CreateClient();

        await HoldApi.AssertErrorAsync(
            await HoldApi.ApplyAsync(client, "c_" + new string('a', 5000), HoldApi.NewStudentId()),
            HttpStatusCode.NotFound, "CLASS_NOT_FOUND");
    }

    [Fact]
    public async Task CreateHold_WhenClassFull_Returns409NoSeatsAvailable()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client, 1);
        Assert.Equal(HttpStatusCode.Created, (await HoldApi.ApplyAsync(client, classId, HoldApi.NewStudentId())).StatusCode);

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.ApplyAsync(client, classId, HoldApi.NewStudentId()), HttpStatusCode.Conflict, "NO_SEATS_AVAILABLE");

        Assert.Equal(classId, user.GetProperty("resourceId").GetString());
    }

    [Fact]
    public async Task CreateHold_ClassFullOfConfirmedHolds_Returns409NoSeatsAvailable()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client, 2);
        await HoldApi.SeedAsync(database, classId, "CONFIRMED", -60);
        await HoldApi.SeedAsync(database, classId, "CONFIRMED", 600);

        await HoldApi.AssertErrorAsync(
            await HoldApi.ApplyAsync(client, classId, HoldApi.NewStudentId()), HttpStatusCode.Conflict, "NO_SEATS_AVAILABLE");
    }

    [Fact]
    public async Task CreateHold_SeatsTakenOnlyByExpiredHolds_Returns201()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client, 2);
        await HoldApi.SeedAsync(database, classId, "HELD", -1);
        await HoldApi.SeedAsync(database, classId, "HELD", -1);

        var response = await HoldApi.ApplyAsync(client, classId, HoldApi.NewStudentId());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateHold_StudentHasActiveHoldInSameClass_Returns409StudentHasActiveHold()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        Assert.Equal(HttpStatusCode.Created, (await HoldApi.ApplyAsync(client, classId, student)).StatusCode);

        var user = await HoldApi.AssertErrorAsync(
            await HoldApi.ApplyAsync(client, classId, student), HttpStatusCode.Conflict, "STUDENT_HAS_ACTIVE_HOLD");

        Assert.Equal(System.Text.Json.JsonValueKind.Null, user.GetProperty("resourceId").ValueKind);
    }

    [Fact]
    public async Task CreateHold_StudentHasActiveHoldInOtherClass_Returns409StudentHasActiveHold()
    {
        var client = _factory.CreateClient();
        var first = await NewClassAsync(client);
        var second = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        Assert.Equal(HttpStatusCode.Created, (await HoldApi.ApplyAsync(client, first, student)).StatusCode);

        await HoldApi.AssertErrorAsync(
            await HoldApi.ApplyAsync(client, second, student), HttpStatusCode.Conflict, "STUDENT_HAS_ACTIVE_HOLD");
    }

    [Fact]
    public async Task CreateHold_StudentHasConfirmedHold_Returns409StudentHasActiveHold()
    {
        var client = _factory.CreateClient();
        var first = await NewClassAsync(client);
        var second = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        await HoldApi.SeedAsync(database, first, "CONFIRMED", -60, student);

        await HoldApi.AssertErrorAsync(
            await HoldApi.ApplyAsync(client, second, student), HttpStatusCode.Conflict, "STUDENT_HAS_ACTIVE_HOLD");
    }

    [Fact]
    public async Task CreateHold_StudentHasActiveHoldAndClassFull_Returns409StudentHasActiveHold()
    {
        var client = _factory.CreateClient();
        var own = await NewClassAsync(client);
        var full = await NewClassAsync(client, 1);
        var student = HoldApi.NewStudentId();
        Assert.Equal(HttpStatusCode.Created, (await HoldApi.ApplyAsync(client, own, student)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await HoldApi.ApplyAsync(client, full, HoldApi.NewStudentId())).StatusCode);

        await HoldApi.AssertErrorAsync(
            await HoldApi.ApplyAsync(client, full, student), HttpStatusCode.Conflict, "STUDENT_HAS_ACTIVE_HOLD");
    }

    [Fact]
    public async Task CreateHold_OwnHeldHoldAlreadyExpired_Returns201AndMarksOldHoldExpired()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        // Time is up but the sweeper has not marked it yet: it must not block the new application.
        var old = await HoldApi.SeedAsync(database, classId, "HELD", -1, student);

        var response = await HoldApi.ApplyAsync(client, classId, student);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("EXPIRED", await HoldApi.StoredStatusAsync(database, old));
    }

    [Theory]
    [InlineData("CANCELLED")]
    [InlineData("EXPIRED")]
    public async Task CreateHold_PreviousHoldCancelled_Returns201(string previousStatus)
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        await HoldApi.SeedAsync(database, classId, previousStatus, 600, student);

        var response = await HoldApi.ApplyAsync(client, classId, student);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateHold_StudentIdWithLeadingZeros_KeepsLeadingZeros()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);
        var student = "0000" + HoldApi.NewStudentId()[4..];

        var body = await TestHost.ReadJsonAsync(await HoldApi.ApplyAsync(client, classId, student));

        Assert.Equal(student, body.GetProperty("studentId").GetString());
        var holdId = HoldApi.GuidOf(body.GetProperty("holdId").GetString()!);
        Assert.Equal(student, await HoldApi.ScalarAsync<string>(database, "SELECT student_id FROM holds WHERE id = @id", ("id", holdId)));
    }

    [Fact]
    public async Task CreateHold_UppercaseHexClassId_Returns201WithLowercaseClassId()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);

        var response = await HoldApi.ApplyAsync(client, "c_" + classId[2..].ToUpperInvariant(), HoldApi.NewStudentId());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(classId, (await TestHost.ReadJsonAsync(response)).GetProperty("classId").GetString());
    }

    [Fact]
    public async Task CreateHold_Rejected_InsertsNoRow()
    {
        var client = _factory.CreateClient();
        var full = await NewClassAsync(client, 1);
        var other = await NewClassAsync(client);
        var holder = HoldApi.NewStudentId();
        var rejected = HoldApi.NewStudentId();
        Assert.Equal(HttpStatusCode.Created, (await HoldApi.ApplyAsync(client, full, holder)).StatusCode);

        Assert.Equal(HttpStatusCode.Conflict, (await HoldApi.ApplyAsync(client, full, rejected)).StatusCode); // no seats
        Assert.Equal(HttpStatusCode.Conflict, (await HoldApi.ApplyAsync(client, other, holder)).StatusCode); // active hold
        Assert.Equal(HttpStatusCode.NotFound, (await HoldApi.ApplyAsync(client, $"c_{Guid.NewGuid():N}", rejected)).StatusCode);

        Assert.Equal(0, await HoldApi.CountAsync(database, "student_id = @s", ("s", rejected)));
        Assert.Equal(1, await HoldApi.CountAsync(database, "student_id = @s", ("s", holder)));
        Assert.Equal(1, await HoldApi.CountAsync(database, "class_id = @c", ("c", HoldApi.GuidOf(full))));
        Assert.Equal(0, await HoldApi.CountAsync(database, "class_id = @c", ("c", HoldApi.GuidOf(other))));
    }

    [Fact]
    public async Task CreateHold_GetOnHoldsCollection_Returns405()
    {
        var client = _factory.CreateClient();
        var classId = await NewClassAsync(client);

        var response = await client.GetAsync($"/classes/{classId}/holds", Ct);

        await HoldApi.AssertErrorAsync(response, HttpStatusCode.MethodNotAllowed, "METHOD_NOT_ALLOWED");
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
