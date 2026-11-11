using SchoolAdmission.Application.Holds;
using SchoolAdmission.Application.Holds.Commands.CancelHold;
using SchoolAdmission.Application.Holds.Commands.ConfirmHold;
using SchoolAdmission.Application.Holds.Commands.CreateHold;

namespace SchoolAdmission.Application.Abstractions;

/// <summary>Persistence for holds; implemented in Infrastructure with raw SQL (concurrency-critical, ADR-0001).</summary>
public interface IHoldRepository
{
    /// <summary>
    /// Tries to hold a seat in one transaction (ARCHITECTURE §5.1): frees the student's own stale holds, checks for an active
    /// hold, locks the class row, counts and inserts. The expiry is <c>now() + holdDurationSeconds</c> on the database clock.
    /// </summary>
    Task<CreateHoldOutcome> CreateAsync(
        Guid classId, string studentId, int holdDurationSeconds, CancellationToken cancellationToken);

    /// <summary>One hold with its class name and effective status (an expired HELD reads as EXPIRED), or null.</summary>
    Task<HoldDto?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Confirms a hold in one transaction (ARCHITECTURE §5.2): locks only the hold row, checks owner and status on the
    /// database clock, then writes CONFIRMED, or EXPIRED when a HELD hold has run out.
    /// </summary>
    Task<ConfirmHoldOutcome> ConfirmAsync(Guid id, string studentId, CancellationToken cancellationToken);

    /// <summary>
    /// Cancels a hold in one transaction (ARCHITECTURE §5.3): locks only the hold row, checks owner and status on the
    /// database clock, then writes CANCELLED, or EXPIRED when a HELD hold has run out.
    /// </summary>
    Task<CancelHoldResult> CancelAsync(Guid id, string studentId, CancellationToken cancellationToken);
}
