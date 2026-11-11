using System.Net.Http.Json;
using System.Text.Json;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Classes;

/// <summary>Small helpers for the classes endpoint tests.</summary>
public static class ClassApi
{
    /// <summary>A prefix unique to one test, so it can pick its own classes out of the shared database.</summary>
    public static string NewPrefix() => $"t{Guid.NewGuid():N}";

    /// <summary>Creates a class and returns the 201 body.</summary>
    public static async Task<JsonElement> CreateAsync(HttpClient client, string name, int totalSeats = 10)
    {
        var response = await client.PostAsJsonAsync(
            "/classes", new { name, totalSeats }, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await TestHost.ReadJsonAsync(response);
    }

    /// <summary>GET /classes, keeping only the items whose name starts with <paramref name="prefix"/>.</summary>
    public static async Task<List<JsonElement>> ListMineAsync(HttpClient client, string prefix)
    {
        var response = await client.GetAsync("/classes", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var all = await TestHost.ReadJsonAsync(response);
        return [.. all.EnumerateArray().Where(c => c.GetProperty("name").GetString()!.StartsWith(prefix, StringComparison.Ordinal))];
    }
}
