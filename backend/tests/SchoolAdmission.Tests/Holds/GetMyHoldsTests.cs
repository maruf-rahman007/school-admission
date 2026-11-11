using System.Net;
using System.Text.Json;
using Npgsql;
using SchoolAdmission.Tests.Classes;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Holds;

[Collection(DatabaseCollection.Name)]
public sealed class GetMyHoldsTests(DatabaseFixture database) : IAsyncDisposable
{
    private const string IsoMillisZ = @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$";

    private readonly DatabaseAppFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<(string ClassId, string Name)> NewClassAsync(HttpClient client)
    {
        var created = await ClassApi.CreateAsync(client, $"{ClassApi.NewPrefix()} Class 7 – Day");
        return (created.GetProperty("id").GetString()!, created.GetProperty("name").GetString()!);
    }

    private static async Task<JsonElement[]> ListAsync(HttpClient client, string studentId)
    {
        var response = await HoldApi.MyHoldsAsync(client, studentId);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        return body.EnumerateArray().ToArray();
    }

    private static string[] HoldIds(JsonElement[] items) => items.Select(i => i.GetProperty("holdId").GetString()!).ToArray();

    [Fact]
    public async Task GetMyHolds_StudentWithHolds_Returns200BareArrayWithExactFields()
    {
        var client = _factory.CreateClient();
        var (classId, name) = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        var created = await TestHost.ReadJsonAsync(await HoldApi.ApplyAsync(client, classId, student));

        var response = await HoldApi.MyHoldsAsync(client, student);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Server-Time", out var serverTime));
        Assert.Matches(IsoMillisZ, serverTime.Single());
        var body = await TestHost.ReadJsonAsync(response);
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        var item = Assert.Single(body.EnumerateArray());
        Assert.Equal(
            ["holdId", "classId", "className", "status", "expiresAt"],
            item.EnumerateObject().Select(p => p.Name));
        Assert.Matches("^h_[0-9a-f]{32}$", item.GetProperty("holdId").GetString());
        Assert.Equal(created.GetProperty("holdId").GetString(), item.GetProperty("holdId").GetString());
        Assert.Matches("^c_[0-9a-f]{32}$", item.GetProperty("classId").GetString());
        Assert.Equal(classId, item.GetProperty("classId").GetString());
        Assert.Equal(name, item.GetProperty("className").GetString());
        Assert.Equal("HELD", item.GetProperty("status").GetString());
        Assert.Matches(IsoMillisZ, item.GetProperty("expiresAt").GetString());
        Assert.Equal(created.GetProperty("expiresAt").GetString(), item.GetProperty("expiresAt").GetString());
    }

    [Fact]
    public async Task GetMyHolds_MultipleHolds_ReturnsNewestFirst()
    {
        var client = _factory.CreateClient();
        var (classId, _) = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        var oldest = await HoldApi.SeedAsync(database, classId, "CANCELLED", 600, student);
        var middle = await HoldApi.SeedAsync(database, classId, "EXPIRED", 600, student);
        var newest = await HoldApi.SeedAsync(database, classId, "HELD", 600, student);
        await HoldApi.SetCreatedAtAsync(database, oldest, -300);
        await HoldApi.SetCreatedAtAsync(database, middle, -200);
        await HoldApi.SetCreatedAtAsync(database, newest, -100);

        var items = await ListAsync(client, student);

        Assert.Equal([HoldApi.HoldIdOf(newest), HoldApi.HoldIdOf(middle), HoldApi.HoldIdOf(oldest)], HoldIds(items));
    }

    [Fact]
    public async Task GetMyHolds_SameCreatedAt_OrdersByIdDescending()
    {
        var client = _factory.CreateClient();
        var (classId, _) = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        var first = await HoldApi.SeedAsync(database, classId, "CANCELLED", 600, student);
        var second = await HoldApi.SeedAsync(database, classId, "EXPIRED", 600, student);
        await using (var connection = new NpgsqlConnection(database.ConnectionString))
        {
            await connection.OpenAsync(Ct);
            await using var command = new NpgsqlCommand(
                "UPDATE holds SET created_at = '2026-01-01T00:00:00Z' WHERE id = ANY(@ids)", connection);
            command.Parameters.AddWithValue("ids", new[] { first, second });
            await command.ExecuteNonQueryAsync(Ct);
        }

        var expected = new[] { first, second }.OrderByDescending(id => id.ToString("N"), StringComparer.Ordinal)
            .Select(HoldApi.HoldIdOf).ToArray();
        Assert.Equal(expected, HoldIds(await ListAsync(client, student)));
        // Stable between calls.
        Assert.Equal(expected, HoldIds(await ListAsync(client, student)));
    }

    [Fact]
    public async Task GetMyHolds_OtherStudentsHolds_AreNotReturned()
    {
        var client = _factory.CreateClient();
        var (classId, _) = await NewClassAsync(client);
        var me = HoldApi.NewStudentId();
        var mine = await HoldApi.SeedAsync(database, classId, "HELD", 600, me);
        await HoldApi.SeedAsync(database, classId, "HELD", 600);
        await HoldApi.SeedAsync(database, classId, "CANCELLED", 600);

        Assert.Equal([HoldApi.HoldIdOf(mine)], HoldIds(await ListAsync(client, me)));
    }

    [Fact]
    public async Task GetMyHolds_HeldHoldPastExpiry_ReturnsExpiredWithoutChangingRow()
    {
        var client = _factory.CreateClient();
        var (classId, _) = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        var holdId = await HoldApi.SeedAsync(database, classId, "HELD", -1, student);

        var item = Assert.Single(await ListAsync(client, student));

        Assert.Equal("EXPIRED", item.GetProperty("status").GetString());
        Assert.Equal("HELD", await HoldApi.StoredStatusAsync(database, holdId));
    }

    [Theory]
    [InlineData("CONFIRMED")]
    [InlineData("CANCELLED")]
    [InlineData("EXPIRED")]
    [InlineData("HELD")]
    public async Task GetMyHolds_StoredStatuses_ReturnedAsStored(string status)
    {
        var client = _factory.CreateClient();
        var (classId, _) = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        await HoldApi.SeedAsync(database, classId, status, 600, student);

        var item = Assert.Single(await ListAsync(client, student));

        Assert.Equal(status, item.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetMyHolds_NoHolds_Returns200EmptyArray()
    {
        var client = _factory.CreateClient();

        Assert.Empty(await ListAsync(client, HoldApi.NewStudentId()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567890123456")]
    [InlineData("123456789012345678")]
    [InlineData("1234567890123456a")]
    [InlineData("১২৩৪৫৬৭৮৯০১২৩৪৫৬৭")]
    public async Task GetMyHolds_InvalidStudentIdHeader_Returns400InvalidStudentId(string? header)
    {
        var client = _factory.CreateClient();

        var response = await HoldApi.MyHoldsAsync(client, header);

        await HoldApi.AssertErrorAsync(response, HttpStatusCode.BadRequest, "INVALID_STUDENT_ID");
    }

    [Fact]
    public async Task GetMyHolds_DuplicateStudentIdHeader_Returns400InvalidStudentId()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/me/holds");
        request.Headers.TryAddWithoutValidation("X-Student-Id", [HoldApi.NewStudentId(), HoldApi.NewStudentId()]);

        var response = await client.SendAsync(request, Ct);

        await HoldApi.AssertErrorAsync(response, HttpStatusCode.BadRequest, "INVALID_STUDENT_ID");
    }

    [Fact]
    public async Task GetMyHolds_LeadingZeroStudentId_ReturnsOnlyOwnHolds()
    {
        var client = _factory.CreateClient();
        var (classId, _) = await NewClassAsync(client);
        var digits = HoldApi.NewStudentId()[..14];
        var withZeros = "000" + digits;
        var withoutZeros = digits + "000";
        var mine = await HoldApi.SeedAsync(database, classId, "HELD", 600, withZeros);
        await HoldApi.SeedAsync(database, classId, "HELD", 600, withoutZeros);

        Assert.Equal([HoldApi.HoldIdOf(mine)], HoldIds(await ListAsync(client, withZeros)));
    }

    [Fact]
    public async Task GetMyHolds_AfterExpireAndReapply_ReturnsBothNewestFirst()
    {
        var client = _factory.CreateClient();
        var (classId, _) = await NewClassAsync(client);
        var student = HoldApi.NewStudentId();
        var expired = await HoldApi.SeedAsync(database, classId, "HELD", -1, student);
        await HoldApi.SetCreatedAtAsync(database, expired, -60);
        var reapplied = await TestHost.ReadJsonAsync(await HoldApi.ApplyAsync(client, classId, student));

        var items = await ListAsync(client, student);

        Assert.Equal([reapplied.GetProperty("holdId").GetString()!, HoldApi.HoldIdOf(expired)], HoldIds(items));
        Assert.Equal("HELD", items[0].GetProperty("status").GetString());
        Assert.Equal("EXPIRED", items[1].GetProperty("status").GetString());
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
