# ADR-0001: Seat concurrency with a Postgres row lock

- **Status:** Accepted (2026-11-10)

## Context
Held + confirmed seats must never exceed `totalSeats`, even when many students apply at the same moment and when two backend processes share the database. Each student may also have only one active (HELD or CONFIRMED) hold across all classes.

## Options considered
1. **Row lock on the class** (`SELECT … FROM classes WHERE id = $1 FOR UPDATE`, count active holds, insert). Simple, and correct across processes because the lock lives in the database.
2. **Atomic conditional insert** (`INSERT … SELECT … WHERE (count) < total_seats`). Not safe on its own under `READ COMMITTED`: two transactions can both see the same count.
3. **Counter column** (`UPDATE classes SET held = held + 1 WHERE held + confirmed < total_seats`). Fast, but the counter must be kept in step with expiry and cancellation, which adds a second source of truth.
4. **Redis** (atomic counters / locks). A second store to run and keep consistent with Postgres; not needed at this scale.

## Decision
Use option 1. All hold creations for one class run one after another inside short transactions. The one-active-hold rule is enforced by a partial unique index, `UNIQUE (student_id) WHERE status IN ('HELD','CONFIRMED')`. A unique violation (`23505`) becomes `409 STUDENT_HAS_ACTIVE_HOLD`. Before inserting, the transaction marks the student's own expired HELD rows as EXPIRED so they do not block the index.

## Consequences
- Correct with any number of backend instances, with no extra infrastructure.
- Hold creation for a single class is serialized. The transaction is a few milliseconds long, which is fine for a school's admission rush.
- Confirm does not need the class lock (HELD → CONFIRMED keeps the total unchanged); it locks only the hold row.
