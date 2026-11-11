using System.Net;
using System.Text.Json;
using Npgsql;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Holds;

/// <summary>Helpers for hold tests: unique student IDs, API calls and direct SQL seeding/reading of <c>holds</c>.</summary>
public static class HoldApi
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// A random 17-digit ID. Every test uses fresh IDs: the one-active-hold index is global, and all tests share admission_test.
    /// </summary>
    public static string NewStudentId() => Random.Shared.NextInt64(10_000_000_000_000_000, 100_000_000_000_000_000).ToString();

    /// <summary>The Guid inside a prefixed id such as <c>c_…</c> or <c>h_…</c>.</summary>
    public static Guid GuidOf(string prefixedId) => Guid.ParseExact(prefixedId[2..], "N");

    /// <summary>POST /classes/{classId}/holds with the given header value (null = no header).</summary>
    public static Task<HttpResponseMessage> ApplyAsync(HttpClient client, string classId, string? studentId) =>
        SendAsync(client, HttpMethod.Post, $"/classes/{classId}/holds", studentId);

    /// <summary>The prefixed hold ID (<c>h_</c> + 32 lowercase hex) for a hold row's Guid.</summary>
    public static string HoldIdOf(Guid id) => $"h_{id:N}";

    /// <summary>POST /holds/{holdId}/confirm with the given header value (null = no header).</summary>
    public static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, string holdId, string? studentId) =>
        SendAsync(client, HttpMethod.Post, $"/holds/{holdId}/confirm", studentId);

    /// <summary>DELETE /holds/{holdId} with the given header value (null = no header).</summary>
    public static Task<HttpResponseMessage> CancelAsync(HttpClient client, string holdId, string? studentId) =>
        SendAsync(client, HttpMethod.Delete, $"/holds/{holdId}", studentId);

    /// <summary>Starts every request, releases them together through one gate, and waits for all responses.</summary>
    public static async Task<(HttpStatusCode Status, string? Code, string Body)[]> FireTogetherAsync(
        IEnumerable<Func<Task<HttpResponseMessage>>> requests)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = requests.Select(async send =>
        {
            await gate.Task;
            var response = await send();
            var body = await response.Content.ReadAsStringAsync(Ct);
            string? code = null;
            if (!response.IsSuccessStatusCode)
            {
                code = (await TestHost.ReadJsonAsync(response)).GetProperty("userResponse").GetProperty("code").GetString();
            }

            return (response.StatusCode, code, body);
        }).ToArray();

        gate.SetResult();
        return await Task.WhenAll(tasks);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string url, string? studentId)
    {
        var request = new HttpRequestMessage(method, url);
        if (studentId is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Student-Id", studentId);
        }

        return client.SendAsync(request, Ct);
    }

    /// <summary>Asserts the standard error body with the given status and code; returns <c>userResponse</c>.</summary>
    public static async Task<JsonElement> AssertErrorAsync(HttpResponseMessage response, System.Net.HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var body = await TestHost.ReadJsonAsync(response);
        var user = body.GetProperty("userResponse");
        Assert.Equal(code, user.GetProperty("code").GetString());
        Assert.Equal(0, user.GetProperty("errors").GetArrayLength());
        Assert.Equal(JsonValueKind.Object, body.GetProperty("devResponse").ValueKind);
        return user;
    }

    /// <summary>
    /// Inserts a hold row directly, bypassing the API. <paramref name="expiresInSeconds"/> is relative to the database clock
    /// (negative = already expired).
    /// </summary>
    public static async Task<Guid> SeedAsync(
        DatabaseFixture database, string classId, string status, int expiresInSeconds, string? studentId = null)
    {
        var id = Guid.NewGuid();
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO holds (id, class_id, student_id, status, expires_at)
            VALUES (@id, @class, @student, @status, now() + make_interval(secs => @seconds))
            """,
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("class", GuidOf(classId));
        command.Parameters.AddWithValue("student", studentId ?? NewStudentId());
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("seconds", (double)expiresInSeconds);
        await command.ExecuteNonQueryAsync(Ct);
        return id;
    }

    /// <summary>Runs a scalar SQL query against admission_test (for asserting what is really stored).</summary>
    public static async Task<T> ScalarAsync<T>(DatabaseFixture database, string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return (T)(await command.ExecuteScalarAsync(Ct))!;
    }

    public static Task<string> StoredStatusAsync(DatabaseFixture database, Guid holdId) =>
        ScalarAsync<string>(database, "SELECT status FROM holds WHERE id = @id", ("id", holdId));

    public static Task<long> CountAsync(DatabaseFixture database, string whereSql, params (string Name, object Value)[] parameters) =>
        ScalarAsync<long>(database, $"SELECT count(*) FROM holds WHERE {whereSql}", parameters);
}
