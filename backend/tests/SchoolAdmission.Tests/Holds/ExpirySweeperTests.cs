using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolAdmission.Application.Common;
using SchoolAdmission.Infrastructure.Expiry;
using SchoolAdmission.Infrastructure.Persistence;
using SchoolAdmission.Tests.Classes;
using SchoolAdmission.Tests.Infrastructure;

namespace SchoolAdmission.Tests.Holds;

/// <summary>
/// The real sweeper hosted service with a 1 s interval. Each test seeds its own rows (relative to the database clock)
/// and polls what is stored, with a deadline, instead of sleeping a fixed time.
/// </summary>
[Collection(DatabaseCollection.Name)]
public sealed class ExpirySweeperTests(DatabaseFixture database) : IAsyncDisposable
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(8);

    private readonly CapturingLoggerProvider _logs = new();
    private readonly DatabaseAppFactory _app = new(
        database, new Dictionary<string, string?> { ["EXPIRY_SWEEP_INTERVAL_SECONDS"] = "1" });

    private WebApplicationFactory<Program>? _factory;

    private WebApplicationFactory<Program> Factory =>
        _factory ??= _app.WithWebHostBuilder(builder => builder.ConfigureLogging(logging => logging.AddProvider(_logs)));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sweeper_HeldHoldPastExpiry_StoredAsExpired()
    {
        var classId = await NewClassAsync();
        var holdId = await HoldApi.SeedAsync(database, classId, "HELD", -1);

        await WaitUntilStoredAsync(holdId, "EXPIRED");
    }

    [Fact]
    public async Task Sweeper_HeldHoldNotYetExpired_StaysHeld()
    {
        var classId = await NewClassAsync();
        var fresh = await HoldApi.SeedAsync(database, classId, "HELD", 600);
        var control = await HoldApi.SeedAsync(database, classId, "HELD", -1);

        // Once the control row is swept, at least one sweep has run over the fresh row too.
        await WaitUntilStoredAsync(control, "EXPIRED");

        Assert.Equal("HELD", await HoldApi.StoredStatusAsync(database, fresh));
    }

    [Fact]
    public async Task Sweeper_ConfirmedAndCancelledHolds_Unchanged()
    {
        var classId = await NewClassAsync();
        var confirmed = await HoldApi.SeedAsync(database, classId, "CONFIRMED", -1);
        var cancelled = await HoldApi.SeedAsync(database, classId, "CANCELLED", -1);
        var control = await HoldApi.SeedAsync(database, classId, "HELD", -1);

        await WaitUntilStoredAsync(control, "EXPIRED");

        Assert.Equal("CONFIRMED", await HoldApi.StoredStatusAsync(database, confirmed));
        Assert.Equal("CANCELLED", await HoldApi.StoredStatusAsync(database, cancelled));
    }

    [Fact]
    public async Task Sweeper_RunTwice_IsIdempotent()
    {
        var classId = await NewClassAsync();
        var holdId = await HoldApi.SeedAsync(database, classId, "HELD", -1);
        await WaitUntilStoredAsync(holdId, "EXPIRED");

        // Let at least two more sweeps run over the already-expired row.
        await Task.Delay(TimeSpan.FromSeconds(2.5), Ct);

        Assert.Equal("EXPIRED", await HoldApi.StoredStatusAsync(database, holdId));
        Assert.Equal(1, await HoldApi.CountAsync(database, "id = @id", ("id", holdId)));
        Assert.DoesNotContain(_logs.Entries, e => e.Level >= LogLevel.Warning && e.Message.Contains("Expiry sweep"));
    }

    [Fact]
    public async Task Sweeper_DatabaseError_LogsWarningAndKeepsRunning()
    {
        // Built directly against a closed port: it can never reach real data.
        var logs = new CapturingLoggerProvider();
        var services = new ServiceCollection()
            .AddLogging(logging => logging.AddProvider(logs))
            .AddDbContext<AdmissionDbContext>(options => options.UseNpgsql(
                "Host=127.0.0.1;Port=1;Database=admission_test;Username=nobody;Password=nothing;Timeout=1"))
            .BuildServiceProvider();
        await using var _ = services;
        var sweeper = new ExpirySweeper(
            services.GetRequiredService<IServiceScopeFactory>(),
            new HoldOptions { HoldDurationSeconds = 600, ExpirySweepIntervalSeconds = 1 },
            services.GetRequiredService<ILogger<ExpirySweeper>>());

        await sweeper.StartAsync(Ct);
        var stopwatch = Stopwatch.StartNew();
        while (Warnings(logs) < 2 && stopwatch.Elapsed < TimeSpan.FromSeconds(15))
        {
            await Task.Delay(200, Ct);
        }

        Assert.True(Warnings(logs) >= 2, "Expected the sweeper to log a warning on at least two ticks.");
        Assert.False(sweeper.ExecuteTask!.IsCompleted, "The sweeper loop must keep running after a failed sweep.");
        await sweeper.StopAsync(Ct);
        Assert.False(sweeper.ExecuteTask.IsFaulted);
    }

    [Fact]
    public void TestAppFactory_RemovesExpirySweeper()
    {
        using var factory = new TestAppFactory();

        Assert.DoesNotContain(factory.Services.GetServices<IHostedService>(), s => s is ExpirySweeper);
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    private static int Warnings(CapturingLoggerProvider logs) =>
        logs.Entries.Count(e => e.Level == LogLevel.Warning && e.Message.StartsWith("Expiry sweep failed", StringComparison.Ordinal));

    private async Task<string> NewClassAsync()
    {
        // Creating a class through the API also starts the host, and with it the sweeper.
        var created = await ClassApi.CreateAsync(Factory.CreateClient(), $"{ClassApi.NewPrefix()} Sweeper");
        return created.GetProperty("id").GetString()!;
    }

    private async Task WaitUntilStoredAsync(Guid holdId, string status)
    {
        var stopwatch = Stopwatch.StartNew();
        while (await HoldApi.StoredStatusAsync(database, holdId) != status)
        {
            Assert.True(stopwatch.Elapsed < Deadline, $"Hold {holdId} was not stored as {status} within {Deadline}.");
            await Task.Delay(200, Ct);
        }
    }
}
