using System.Net;
using System.Text.Json;
using SchoolAdmission.Tests.Classes;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Holds;

/// <summary>
/// Confirm and cancel requests at the same moment, through the real HTTP pipeline and PostgreSQL (ARCHITECTURE §5.2–5.3):
/// the hold-row lock must make a repeat confirm count once and let exactly one of confirm/cancel win.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class ConfirmCancelConcurrencyTests(DatabaseFixture database) : IAsyncDisposable
{
    private const int ParallelConfirms = 20;

    private const int RaceRounds = 10;

    private readonly DatabaseAppFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ConfirmHold_ManyParallelConfirmsOfOneHold_AllReturn200AndConfirmedSeatsIncreasesOnce()
    {
        var client = _factory.CreateClient();
        var (classId, holdId, student) = await ApplyAsync(client);

        var results = await HoldApi.FireTogetherAsync(Enumerable.Range(0, ParallelConfirms)
            .Select(_ => (Func<Task<HttpResponseMessage>>)(() => HoldApi.ConfirmAsync(client, holdId, student))));

        Assert.All(results, r =>
        {
            Assert.Equal(HttpStatusCode.OK, r.Status);
            var body = JsonDocument.Parse(r.Body).RootElement;
            Assert.Equal(holdId, body.GetProperty("holdId").GetString());
            Assert.Equal("CONFIRMED", body.GetProperty("status").GetString());
        });

        var id = HoldApi.GuidOf(holdId);
        Assert.Equal(1, await HoldApi.CountAsync(database, "id = @id", ("id", id)));
        Assert.Equal("CONFIRMED", await HoldApi.StoredStatusAsync(database, id));
        var confirmedAt = await ConfirmedAtAsync(id);

        var c = await GetJsonAsync(client, $"/classes/{classId}");
        Assert.Equal(1, c.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(0, c.GetProperty("heldSeats").GetInt32());

        // A later confirm does not overwrite confirmed_at.
        Assert.Equal(HttpStatusCode.OK, (await HoldApi.ConfirmAsync(client, holdId, student)).StatusCode);
        Assert.Equal(confirmedAt, await ConfirmedAtAsync(id));
    }

    [Fact]
    public async Task ConfirmHold_ParallelConfirmAndCancel_ExactlyOneWinsOtherGets409()
    {
        var client = _factory.CreateClient();

        for (var round = 0; round < RaceRounds; round++)
        {
            var (classId, holdId, student) = await ApplyAsync(client);

            var results = await HoldApi.FireTogetherAsync(
            [
                () => HoldApi.ConfirmAsync(client, holdId, student),
                () => HoldApi.CancelAsync(client, holdId, student),
            ]);
            var (confirm, cancel) = (results[0], results[1]);

            var id = HoldApi.GuidOf(holdId);
            var stored = await HoldApi.StoredStatusAsync(database, id);
            var c = await GetJsonAsync(client, $"/classes/{classId}");
            Assert.Equal(0, c.GetProperty("heldSeats").GetInt32());

            if (confirm.Status == HttpStatusCode.OK)
            {
                Assert.Equal(HttpStatusCode.Conflict, cancel.Status);
                Assert.Equal("HOLD_ALREADY_CONFIRMED", cancel.Code);
                Assert.Equal("CONFIRMED", stored);
                Assert.Equal(1, await HoldApi.CountAsync(
                    database, "id = @id AND confirmed_at IS NOT NULL AND cancelled_at IS NULL", ("id", id)));
                Assert.Equal(1, c.GetProperty("confirmedSeats").GetInt32());
            }
            else
            {
                Assert.Equal(HttpStatusCode.Conflict, confirm.Status);
                Assert.Equal("HOLD_CANCELLED", confirm.Code);
                Assert.Equal(HttpStatusCode.NoContent, cancel.Status);
                Assert.Equal("CANCELLED", stored);
                Assert.Equal(1, await HoldApi.CountAsync(
                    database, "id = @id AND cancelled_at IS NOT NULL AND confirmed_at IS NULL", ("id", id)));
                Assert.Equal(0, c.GetProperty("confirmedSeats").GetInt32());
            }
        }
    }

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    /// <summary>A new class and a HELD hold in it for a fresh student, created through the API.</summary>
    private static async Task<(string ClassId, string HoldId, string StudentId)> ApplyAsync(HttpClient client)
    {
        var classId = (await ClassApi.CreateAsync(client, $"{ClassApi.NewPrefix()} Race", 5)).GetProperty("id").GetString()!;
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

    private Task<DateTime> ConfirmedAtAsync(Guid id) =>
        HoldApi.ScalarAsync<DateTime>(database, "SELECT confirmed_at FROM holds WHERE id = @id", ("id", id));
}
