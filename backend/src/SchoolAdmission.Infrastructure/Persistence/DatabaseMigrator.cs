using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SchoolAdmission.Infrastructure.Persistence;

/// <summary>
/// Applies pending EF Core migrations at startup. Hosted services start before the web server listens,
/// so no request ever sees an unmigrated database. A failure stops startup with
/// <see cref="DatabaseMigrationException"/> (the process then exits with code 1).
/// </summary>
public sealed class DatabaseMigrator(IServiceProvider services, ILogger<DatabaseMigrator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AdmissionDbContext>();

            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            if (pending.Count == 0)
            {
                logger.LogInformation("Database is up to date; no migrations to apply.");
                return;
            }

            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Applied {Count} migration(s): {Migrations}", pending.Count, string.Join(", ", pending));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Never log the connection string (it holds the password); the exception text doesn't contain it.
            logger.LogError(exception, "Applying database migrations failed.");
            throw new DatabaseMigrationException(exception);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Startup could not migrate the database. Its message never contains the connection string.</summary>
public sealed class DatabaseMigrationException(Exception inner)
    : Exception($"Applying database migrations failed: {inner.GetType().Name}: {inner.Message}", inner);
