using System.Net;
using SchoolAdmission.Tests.Classes;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Holds;

/// <summary>
/// Bonus (ARCHITECTURE §9): two independent app instances on one database, as two backend processes would be. Each host has
/// its own DI container and Npgsql pool, so nothing in memory is shared; only PostgreSQL (row lock, unique index) can keep
/// held + confirmed within totalSeats.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class MultiInstanceConcurrencyTests(DatabaseFixture database) : IAsyncDisposable
{
    private readonly DatabaseAppFactory _instanceA = new(database);
    private readonly DatabaseAppFactory _instanceB = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// Starts A, then B, one after the other: each host runs startup migrations, and those must not run at the same time
    /// (the compose profile likewise starts backend2 only after backend is healthy).
    /// </summary>
    private async Task<(HttpClient A, HttpClient B)> StartBothAsync()
    {
        var a = _instanceA.CreateClient();
        (await a.GetAsync("/health", Ct)).EnsureSuccessStatusCode();
        var b = _instanceB.CreateClient();
        (await b.GetAsync("/health", Ct)).EnsureSuccessStatusCode();
        return (a, b);
    }

    [Fact]
    public async Task CreateHold_FiftyParallelRequestsAcrossTwoInstancesForFiveSeats_ExactlyFiveSucceed()
    {
        var (a, b) = await StartBothAsync();
        var classId = (await ClassApi.CreateAsync(a, $"{ClassApi.NewPrefix()} Two instances", 5)).GetProperty("id").GetString()!;
        var students = Enumerable.Range(0, 50).Select(_ => HoldApi.NewStudentId()).Distinct().ToList();
        Assert.Equal(50, students.Count);

        // Interleaved: even indexes through A, odd through B, all released by one gate.
        var clients = students.Select((_, i) => i % 2 == 0 ? a : b).ToList();
        Assert.Equal(25, clients.Count(c => c == a));
        Assert.Equal(25, clients.Count(c => c == b));

        var results = await HoldApi.FireTogetherAsync(
            students.Select((s, i) => (Func<Task<HttpResponseMessage>>)(() => HoldApi.ApplyAsync(clients[i], classId, s))));

        Assert.DoesNotContain(results, r => r.Status == HttpStatusCode.InternalServerError);
        var created = results.Where(r => r.Status == HttpStatusCode.Created).ToList();
        var rejected = results.Where(r => r.Status != HttpStatusCode.Created).ToList();
        Assert.Equal(5, created.Count);
        Assert.Equal(45, rejected.Count);
        Assert.All(rejected, r =>
        {
            Assert.Equal(HttpStatusCode.Conflict, r.Status);
            Assert.Equal("NO_SEATS_AVAILABLE", r.Code);
        });

        // What PostgreSQL really stores.
        Assert.Equal(5, await HoldApi.CountAsync(
            database, "class_id = @c AND status = 'HELD'", ("c", HoldApi.GuidOf(classId))));
        var bodies = created.Select(r => System.Text.Json.JsonDocument.Parse(r.Body).RootElement).ToList();
        Assert.Equal(5, bodies.Select(x => x.GetProperty("holdId").GetString()).Distinct().Count());
        var winners = bodies.Select(x => x.GetProperty("studentId").GetString()!).ToList();
        Assert.Equal(5, winners.Distinct().Count());
        Assert.All(winners, w => Assert.Contains(w, students));

        // Both instances report the same counts.
        foreach (var client in new[] { a, b })
        {
            var c = await TestHost.ReadJsonAsync(await client.GetAsync($"/classes/{classId}", Ct));
            Assert.Equal(5, c.GetProperty("heldSeats").GetInt32());
            Assert.Equal(0, c.GetProperty("confirmedSeats").GetInt32());
            Assert.Equal(0, c.GetProperty("availableSeats").GetInt32());
        }
    }

    [Fact]
    public async Task CreateHold_OneStudentTwoClassesOnTwoInstancesInParallel_ExactlyOneSucceeds()
    {
        var (a, b) = await StartBothAsync();
        var class1 = (await ClassApi.CreateAsync(a, $"{ClassApi.NewPrefix()} Morning", 3)).GetProperty("id").GetString()!;
        var class2 = (await ClassApi.CreateAsync(b, $"{ClassApi.NewPrefix()} Day", 3)).GetProperty("id").GetString()!;
        var student = HoldApi.NewStudentId();

        var results = await HoldApi.FireTogetherAsync(
        [
            () => HoldApi.ApplyAsync(a, class1, student),
            () => HoldApi.ApplyAsync(b, class2, student),
        ]);

        Assert.Single(results, r => r.Status == HttpStatusCode.Created);
        var rejected = Assert.Single(results, r => r.Status != HttpStatusCode.Created);
        Assert.Equal(HttpStatusCode.Conflict, rejected.Status);
        Assert.Equal("STUDENT_HAS_ACTIVE_HOLD", rejected.Code);
        Assert.Equal(1, await HoldApi.CountAsync(
            database, "student_id = @s AND status IN ('HELD','CONFIRMED')", ("s", student)));
    }

    public async ValueTask DisposeAsync()
    {
        await _instanceA.DisposeAsync();
        await _instanceB.DisposeAsync();
    }
}
