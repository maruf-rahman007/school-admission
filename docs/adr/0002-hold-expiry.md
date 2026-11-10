# ADR-0002: Hold expiry at query time plus a background sweeper

- **Status:** Accepted (2026-11-10)

## Context
A HELD hold must expire automatically `HOLD_DURATION_SECONDS` (default 600) after creation, with no API call needed. Expired holds must stop counting as held seats immediately, and their status must read `EXPIRED`.

## Options considered
1. **Query time only.** Every read treats `HELD AND expires_at <= now()` as EXPIRED. Always correct, but stored rows stay `HELD` forever.
2. **Sweeper only.** A background job writes EXPIRED. Simple reads, but between sweeps a hold looks HELD after its time is up, which is wrong for seat counts and confirm.
3. **Both.** Query time decides; the sweeper keeps the stored data tidy.

## Decision
Use option 3.
- Every read and write compares `expires_at` with the **database** `now()`.
- A `BackgroundService` runs `UPDATE holds SET status='EXPIRED' WHERE status='HELD' AND expires_at <= now()` every `EXPIRY_SWEEP_INTERVAL_SECONDS` (default 5).
- The database clock is the only clock for expiry, so there is no `IClock` abstraction. Tests use a short `HOLD_DURATION_SECONDS` and wait in real time.

## Consequences
- Correctness never depends on the sweeper's timing or on how many instances run it (the update is idempotent).
- Seat-count and status queries must always include the `expires_at > now()` condition. Reviews check this.
- Expiry tests take a few seconds of real waiting.
