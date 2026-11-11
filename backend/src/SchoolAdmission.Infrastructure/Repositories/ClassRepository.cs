using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Abstractions;
using SchoolAdmission.Application.Classes;
using SchoolAdmission.Domain;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Infrastructure.Repositories;

public sealed class ClassRepository(AdmissionDbContext db) : IClassRepository
{
    // Seat counts straight from holds (ARCHITECTURE §4), judged by the database clock (ADR-0002):
    // a HELD hold counts only while expires_at > now(), even if the sweeper has not marked it EXPIRED yet.
    private const string SelectWithCounts = """
        SELECT c.id AS "Id",
               c.name AS "Name",
               c.total_seats AS "TotalSeats",
               (SELECT count(*)::int FROM holds h
                 WHERE h.class_id = c.id AND h.status = 'HELD' AND h.expires_at > now()) AS "HeldSeats",
               (SELECT count(*)::int FROM holds h
                 WHERE h.class_id = c.id AND h.status = 'CONFIRMED') AS "ConfirmedSeats"
        FROM classes c
        """;

    public async Task<SchoolClass> AddAsync(SchoolClass schoolClass, CancellationToken cancellationToken)
    {
        db.Classes.Add(schoolClass);
        // EF reads created_at (DEFAULT now()) back in the same INSERT … RETURNING statement.
        await db.SaveChangesAsync(cancellationToken);
        return schoolClass;
    }

    // One statement for all classes; ORDER BY name uses the column's numeric_ci collation.
    public async Task<IReadOnlyList<ClassDto>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Database
            .SqlQueryRaw<ClassRow>(SelectWithCounts + " ORDER BY c.name, c.created_at, c.id")
            .ToListAsync(cancellationToken);
        return [.. rows.Select(ToDto)];
    }

    public async Task<ClassDto?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var rows = await db.Database
            .SqlQueryRaw<ClassRow>(SelectWithCounts + " WHERE c.id = {0}", id)
            .ToListAsync(cancellationToken);
        return rows.Count == 0 ? null : ToDto(rows[0]);
    }

    // Not clamped: held + confirmed never exceeds total through the API, and a negative value would expose a bug.
    private static ClassDto ToDto(ClassRow r) =>
        new(r.Id, r.Name, r.TotalSeats, r.HeldSeats, r.ConfirmedSeats, r.TotalSeats - r.HeldSeats - r.ConfirmedSeats);

    private sealed class ClassRow
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public int TotalSeats { get; init; }

        public int HeldSeats { get; init; }

        public int ConfirmedSeats { get; init; }
    }
}
