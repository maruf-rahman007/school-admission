using System.Text.Json;
using SchoolAdmission.Tests.Holds;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Classes;

/// <summary>Seat counts come from the holds table and follow the database clock (ARCHITECTURE §4).</summary>
[Collection(DatabaseCollection.Name)]
public sealed class SeatCountTests(DatabaseFixture database) : IAsyncDisposable
{
    private readonly DatabaseAppFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<string> NewClassAsync(HttpClient client, int totalSeats, string? name = null) =>
        (await ClassApi.CreateAsync(client, name ?? $"{ClassApi.NewPrefix()} Seats", totalSeats)).GetProperty("id").GetString()!;

    private static async Task<JsonElement> GetClassAsync(HttpClient client, string id)
    {
        var response = await client.GetAsync($"/classes/{id}", Ct);
        response.EnsureSuccessStatusCode();
        return await TestHost.ReadJsonAsync(response);
    }

    private static void AssertCounts(JsonElement c, int held, int confirmed, int available)
    {
        Assert.Equal(held, c.GetProperty("heldSeats").GetInt32());
        Assert.Equal(confirmed, c.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(available, c.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task GetClassById_NoHolds_ReturnsZeroHeldAndConfirmed()
    {
        var client = _factory.CreateClient();
        var id = await NewClassAsync(client, 7);

        AssertCounts(await GetClassAsync(client, id), held: 0, confirmed: 0, available: 7);
    }

    [Fact]
    public async Task GetClassById_ActiveHeldHolds_CountsAsHeldSeats()
    {
        var client = _factory.CreateClient();
        var id = await NewClassAsync(client, 10);
        await HoldApi.SeedAsync(database, id, "HELD", 600);
        await HoldApi.SeedAsync(database, id, "HELD", 600);

        AssertCounts(await GetClassAsync(client, id), held: 2, confirmed: 0, available: 8);
    }

    [Fact]
    public async Task GetClassById_ExpiredHeldHold_NotCountedAndSeatAvailable()
    {
        var client = _factory.CreateClient();
        var id = await NewClassAsync(client, 1);
        // Still stored as HELD (no sweeper run), but its time is up.
        await HoldApi.SeedAsync(database, id, "HELD", -1);

        AssertCounts(await GetClassAsync(client, id), held: 0, confirmed: 0, available: 1);
    }

    [Fact]
    public async Task GetClassById_ConfirmedHolds_CountsAsConfirmedSeats()
    {
        var client = _factory.CreateClient();
        var id = await NewClassAsync(client, 10);
        await HoldApi.SeedAsync(database, id, "CONFIRMED", 600);
        // A confirmed admission stays counted after its original hold time has passed.
        await HoldApi.SeedAsync(database, id, "CONFIRMED", -600);

        AssertCounts(await GetClassAsync(client, id), held: 0, confirmed: 2, available: 8);
    }

    [Fact]
    public async Task GetClassById_ExpiredAndCancelledHolds_NotCounted()
    {
        var client = _factory.CreateClient();
        var id = await NewClassAsync(client, 5);
        await HoldApi.SeedAsync(database, id, "EXPIRED", -60);
        await HoldApi.SeedAsync(database, id, "CANCELLED", 600);

        AssertCounts(await GetClassAsync(client, id), held: 0, confirmed: 0, available: 5);
    }

    [Fact]
    public async Task GetClassById_MixedHolds_AvailableIsTotalMinusHeldMinusConfirmed()
    {
        var client = _factory.CreateClient();
        var id = await NewClassAsync(client, 10);
        await HoldApi.SeedAsync(database, id, "HELD", 600);
        await HoldApi.SeedAsync(database, id, "HELD", 600);
        await HoldApi.SeedAsync(database, id, "HELD", -5);
        await HoldApi.SeedAsync(database, id, "CONFIRMED", 600);
        await HoldApi.SeedAsync(database, id, "CONFIRMED", 600);
        await HoldApi.SeedAsync(database, id, "CONFIRMED", 600);
        await HoldApi.SeedAsync(database, id, "CANCELLED", 600);
        await HoldApi.SeedAsync(database, id, "EXPIRED", -5);

        AssertCounts(await GetClassAsync(client, id), held: 2, confirmed: 3, available: 5);
    }

    [Fact]
    public async Task GetClasses_MixedHolds_ReturnsSameCountsAsGetClassById()
    {
        var client = _factory.CreateClient();
        var prefix = ClassApi.NewPrefix();
        var id = await NewClassAsync(client, 6, $"{prefix} Mixed");
        await HoldApi.SeedAsync(database, id, "HELD", 600);
        await HoldApi.SeedAsync(database, id, "HELD", -5);
        await HoldApi.SeedAsync(database, id, "CONFIRMED", 600);

        var single = await GetClassAsync(client, id);
        var listed = Assert.Single(await ClassApi.ListMineAsync(client, prefix));

        AssertCounts(single, held: 1, confirmed: 1, available: 4);
        AssertCounts(listed, held: 1, confirmed: 1, available: 4);
    }

    [Fact]
    public async Task GetClasses_HoldsInOtherClass_DoNotAffectCounts()
    {
        var client = _factory.CreateClient();
        var prefix = ClassApi.NewPrefix();
        var busy = await NewClassAsync(client, 3, $"{prefix} A busy");
        var quiet = await NewClassAsync(client, 3, $"{prefix} B quiet");
        await HoldApi.SeedAsync(database, busy, "HELD", 600);
        await HoldApi.SeedAsync(database, busy, "CONFIRMED", 600);

        var mine = await ClassApi.ListMineAsync(client, prefix);

        Assert.Equal([busy, quiet], mine.Select(c => c.GetProperty("id").GetString()));
        AssertCounts(mine[0], held: 1, confirmed: 1, available: 1);
        AssertCounts(mine[1], held: 0, confirmed: 0, available: 3);
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
