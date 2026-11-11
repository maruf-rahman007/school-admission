using System.Net;
using SchoolAdmission.Tests.Classes;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Holds;

/// <summary>
/// Many requests at the same moment, through the real HTTP pipeline and PostgreSQL (ARCHITECTURE §12, spec §1.7):
/// the class row lock and the partial unique index must hold, with no overbooking and no 500.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class CreateHoldConcurrencyTests(DatabaseFixture database) : IAsyncDisposable
{
    private readonly DatabaseAppFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CreateHold_FiftyParallelRequestsForFiveSeats_ExactlyFiveSucceed()
    {
        var client = _factory.CreateClient();
        var classId = (await ClassApi.CreateAsync(client, $"{ClassApi.NewPrefix()} Rush", 5)).GetProperty("id").GetString()!;
        var students = Enumerable.Range(0, 50).Select(_ => HoldApi.NewStudentId()).Distinct().ToList();
        Assert.Equal(50, students.Count);

        var results = await HoldApi.FireTogetherAsync(
            students.Select(s => (Func<Task<HttpResponseMessage>>)(() => HoldApi.ApplyAsync(client, classId, s))));

        var created = results.Where(r => r.Status == HttpStatusCode.Created).ToList();
        var rejected = results.Where(r => r.Status != HttpStatusCode.Created).ToList();
        Assert.Equal(5, created.Count);
        Assert.Equal(45, rejected.Count);
        Assert.All(rejected, r =>
        {
            Assert.Equal(HttpStatusCode.Conflict, r.Status);
            Assert.Equal("NO_SEATS_AVAILABLE", r.Code);
        });

        // What PostgreSQL really stores, not only what the API says.
        Assert.Equal(5, await HoldApi.CountAsync(
            database, "class_id = @c AND status = 'HELD'", ("c", HoldApi.GuidOf(classId))));

        var bodies = created.Select(r => System.Text.Json.JsonDocument.Parse(r.Body).RootElement).ToList();
        Assert.Equal(5, bodies.Select(b => b.GetProperty("holdId").GetString()).Distinct().Count());
        var winners = bodies.Select(b => b.GetProperty("studentId").GetString()!).ToList();
        Assert.Equal(5, winners.Distinct().Count());
        Assert.All(winners, w => Assert.Contains(w, students));

        var c = await TestHost.ReadJsonAsync(await client.GetAsync($"/classes/{classId}", Ct));
        Assert.Equal(5, c.GetProperty("heldSeats").GetInt32());
        Assert.Equal(0, c.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(0, c.GetProperty("availableSeats").GetInt32());
    }

    [Fact]
    public async Task CreateHold_OneStudentTenClassesInParallel_ExactlyOneSucceeds()
    {
        var client = _factory.CreateClient();
        var classIds = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            classIds.Add((await ClassApi.CreateAsync(client, $"{ClassApi.NewPrefix()} Shift {i}", 3)).GetProperty("id").GetString()!);
        }

        var student = HoldApi.NewStudentId();

        var results = await HoldApi.FireTogetherAsync(
            classIds.Select(id => (Func<Task<HttpResponseMessage>>)(() => HoldApi.ApplyAsync(client, id, student))));

        Assert.Single(results, r => r.Status == HttpStatusCode.Created);
        var rejected = results.Where(r => r.Status != HttpStatusCode.Created).ToList();
        Assert.Equal(9, rejected.Count);
        Assert.All(rejected, r =>
        {
            Assert.Equal(HttpStatusCode.Conflict, r.Status);
            Assert.Equal("STUDENT_HAS_ACTIVE_HOLD", r.Code);
        });

        Assert.Equal(1, await HoldApi.CountAsync(
            database, "student_id = @s AND status IN ('HELD','CONFIRMED')", ("s", student)));
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
