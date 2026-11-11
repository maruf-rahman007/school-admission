using System.Net;
using System.Text.Json;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Classes;

[Collection(DatabaseCollection.Name)]
public sealed class GetClassesTests(DatabaseFixture database) : IAsyncDisposable
{
    private readonly DatabaseAppFactory _factory = new(database);

    private async Task<List<string>> NamesInOrderAsync(string prefix, params string[] suffixes)
    {
        var client = _factory.CreateClient();
        foreach (var suffix in suffixes)
        {
            await ClassApi.CreateAsync(client, $"{prefix} {suffix}");
        }

        return [.. (await ClassApi.ListMineAsync(client, prefix)).Select(c => c.GetProperty("name").GetString()![(prefix.Length + 1)..])];
    }

    [Fact]
    public async Task GetClasses_Always_ReturnsBareJsonArray()
    {
        var response = await _factory.CreateClient().GetAsync("/classes", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Array, (await TestHost.ReadJsonAsync(response)).ValueKind);
        Assert.True(response.Headers.Contains("X-Server-Time"));
    }

    [Fact]
    public async Task GetClasses_AfterCreate_ContainsClassWithZeroCountsAndFullAvailability()
    {
        var client = _factory.CreateClient();
        var prefix = ClassApi.NewPrefix();
        var created = await ClassApi.CreateAsync(client, $"{prefix} Class 6", 120);

        var item = Assert.Single(await ClassApi.ListMineAsync(client, prefix));

        Assert.Equal(
            ["id", "name", "totalSeats", "heldSeats", "confirmedSeats", "availableSeats"],
            item.EnumerateObject().Select(p => p.Name));
        Assert.Equal(created.GetProperty("id").GetString(), item.GetProperty("id").GetString());
        Assert.Equal(120, item.GetProperty("totalSeats").GetInt32());
        Assert.Equal(0, item.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, item.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(120, item.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task GetClasses_NumberedNames_SortsNumberAware() =>
        Assert.Equal(
            ["Class 6", "Class 7", "Class 9", "Class 10", "Class 100"],
            await NamesInOrderAsync(ClassApi.NewPrefix(), "Class 10", "Class 100", "Class 6", "Class 9", "Class 7"));

    [Fact]
    public async Task GetClasses_MixedCaseNames_SortsCaseInsensitive() =>
        Assert.Equal(
            ["Apple", "banana", "Cherry", "class 8", "Class 9"],
            await NamesInOrderAsync(ClassApi.NewPrefix(), "Cherry", "class 8", "banana", "Class 9", "Apple"));

    [Fact]
    public async Task GetClasses_SameName_SortsByCreationTime()
    {
        var client = _factory.CreateClient();
        var prefix = ClassApi.NewPrefix();
        var ids = new List<string?>();
        for (var i = 0; i < 3; i++)
        {
            ids.Add((await ClassApi.CreateAsync(client, $"{prefix} Twin")).GetProperty("id").GetString());
        }

        var listed = (await ClassApi.ListMineAsync(client, prefix)).Select(c => c.GetProperty("id").GetString());

        Assert.Equal(ids, listed);
    }

    [Fact]
    public async Task GetClasses_NamesDifferingOnlyInCase_SortsByCreationTime() =>
        Assert.Equal(
            ["class 6", "CLASS 6", "Class 6"],
            await NamesInOrderAsync(ClassApi.NewPrefix(), "class 6", "CLASS 6", "Class 6"));

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
