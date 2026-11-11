using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SchoolAdmission.Application.Common;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Infrastructure.Expiry;

/// <summary>
/// Bookkeeping for hold expiry (ADR-0002): every <see cref="HoldOptions.ExpirySweepIntervalSeconds"/> it stores
/// <c>EXPIRED</c> on HELD holds whose time ran out on the database clock. Reads never depend on it (query-time expiry
/// is the source of truth). The update is idempotent, so any number of backend instances can run it at once.
/// A failed sweep is logged and retried on the next tick; it never stops the host.
/// </summary>
public sealed class ExpirySweeper(IServiceScopeFactory scopes, HoldOptions options, ILogger<ExpirySweeper> logger)
    : BackgroundService
{
    private const string SweepSql =
        "UPDATE holds SET status = 'EXPIRED' WHERE status = 'HELD' AND expires_at <= now()";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The first sweep runs after one interval; each sweep is awaited, so sweeps never overlap.
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.ExpirySweepIntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SweepAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }

    private async Task SweepAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();
            var expired = await db.Database.ExecuteSqlRawAsync(SweepSql, stoppingToken);
            if (expired > 0)
            {
                logger.LogDebug("Expiry sweep marked {Count} hold(s) EXPIRED.", expired);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // The exception text never contains the connection string (same rule as DatabaseMigrator).
            logger.LogWarning(
                exception, "Expiry sweep failed; retrying in {Seconds} s.", options.ExpirySweepIntervalSeconds);
        }
    }
}
