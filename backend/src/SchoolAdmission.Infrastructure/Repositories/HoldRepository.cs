using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.Holds;
using SchoolAdmission.Application.Holds.Commands.CreateHold;
using SchoolAdmission.Domain;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Infrastructure.Repositories;

/// <summary>
/// Holds with raw, parameterised SQL. Every rule is enforced inside PostgreSQL (ADR-0001, ADR-0002):
/// the class row lock serialises seat counting, the partial unique index allows one active hold per student,
/// and every expiry comparison uses the database clock <c>now()</c>.
/// </summary>
public sealed class HoldRepository(AdmissionDbContext db) : IHoldRepository
{
    private const string ActiveHoldIndex = "ux_holds_one_active_per_student";

    public async Task<CreateHoldOutcome> CreateAsync(
        Guid classId, string studentId, int holdDurationSeconds, CancellationToken cancellationToken)
    {
        // READ COMMITTED (the default). Lock order is always: own stale holds → class row → insert (no deadlocks).
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Free the student's own holds that ran out but were not swept yet, so they don't block the unique index.
            await ExecuteAsync(
                transaction,
                "UPDATE holds SET status = 'EXPIRED' WHERE student_id = @student AND status = 'HELD' AND expires_at <= now()",
                cancellationToken,
                ("student", studentId));

            // 2. Friendly pre-check; the unique index (step 5) is the real guarantee.
            var hasActive = (bool)(await ScalarAsync(
                transaction,
                "SELECT EXISTS (SELECT 1 FROM holds WHERE student_id = @student AND status IN ('HELD','CONFIRMED'))",
                cancellationToken,
                ("student", studentId)))!;
            if (hasActive)
            {
                return await RollbackAsync(transaction, CreateHoldResult.StudentHasActiveHold, cancellationToken);
            }

            // 3. Lock the class row: all hold creations for this class now run one at a time.
            var totalSeats = await ScalarAsync(
                transaction, "SELECT total_seats FROM classes WHERE id = @class FOR UPDATE", cancellationToken, ("class", classId));
            if (totalSeats is null)
            {
                return await RollbackAsync(transaction, CreateHoldResult.ClassNotFound, cancellationToken);
            }

            // 4. Seats taken right now (database clock).
            var taken = (int)(await ScalarAsync(
                transaction,
                """
                SELECT count(*)::int FROM holds
                WHERE class_id = @class AND ((status = 'HELD' AND expires_at > now()) OR status = 'CONFIRMED')
                """,
                cancellationToken,
                ("class", classId)))!;
            if (taken >= (int)totalSeats)
            {
                return await RollbackAsync(transaction, CreateHoldResult.NoSeatsAvailable, cancellationToken);
            }

            // 5. Insert; expires_at is computed by the database.
            var holdId = Guid.NewGuid();
            var expiresAt = (DateTime)(await ScalarAsync(
                transaction,
                """
                INSERT INTO holds (id, class_id, student_id, status, expires_at)
                VALUES (@id, @class, @student, 'HELD', now() + make_interval(secs => @seconds))
                RETURNING expires_at
                """,
                cancellationToken,
                ("id", holdId), ("class", classId), ("student", studentId), ("seconds", (double)holdDurationSeconds)))!;

            await transaction.CommitAsync(cancellationToken);
            return new CreateHoldOutcome(
                CreateHoldResult.Created,
                new HoldCreatedDto(holdId, classId, studentId, HoldStatus.Held, new DateTimeOffset(expiresAt, TimeSpan.Zero)));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation && ex.ConstraintName == ActiveHoldIndex)
        {
            // A parallel request by the same student inserted first.
            return await RollbackAsync(transaction, CreateHoldResult.StudentHasActiveHold, cancellationToken);
        }
    }

    public async Task<HoldDto?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        // Effective status on the database clock; read only (the sweeper writes EXPIRED, not this query).
        var rows = await db.Database.SqlQueryRaw<HoldRow>(
                """
                SELECT h.id AS "Id",
                       h.class_id AS "ClassId",
                       c.name AS "ClassName",
                       CASE WHEN h.status = 'HELD' AND h.expires_at <= now() THEN 'EXPIRED' ELSE h.status END AS "Status",
                       h.expires_at AS "ExpiresAt"
                FROM holds h
                JOIN classes c ON c.id = h.class_id
                WHERE h.id = {0}
                """,
                id)
            .ToListAsync(cancellationToken);

        return rows.Count == 0
            ? null
            : new HoldDto(rows[0].Id, rows[0].ClassId, rows[0].ClassName, HoldStatusText.FromText(rows[0].Status), rows[0].ExpiresAt);
    }

    private static async Task<CreateHoldOutcome> RollbackAsync(
        IDbContextTransaction transaction, CreateHoldResult result, CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        return new CreateHoldOutcome(result);
    }

    private async Task ExecuteAsync(
        IDbContextTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object Value)[] parameters)
    {
        await using var command = CreateCommand(transaction, sql, parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<object?> ScalarAsync(
        IDbContextTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object Value)[] parameters)
    {
        await using var command = CreateCommand(transaction, sql, parameters);
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is DBNull ? null : value;
    }

    private DbCommand CreateCommand(IDbContextTransaction transaction, string sql, (string Name, object Value)[] parameters)
    {
        var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.Add(new NpgsqlParameter(name, value));
        }

        return command;
    }

    private sealed class HoldRow
    {
        public Guid Id { get; init; }

        public Guid ClassId { get; init; }

        public string ClassName { get; init; } = string.Empty;

        public string Status { get; init; } = string.Empty;

        public DateTimeOffset ExpiresAt { get; init; }
    }
}
