# Assumptions


## Holds & admission
- **One active application per student across all classes.** A student with a HELD or CONFIRMED hold in any class cannot apply to another class (409). A student can only be admitted to one class/shift, and blocking parallel holds stops one family from tying up seats in several shifts during the rush.
- **Re-applying is allowed** right after a hold expires or is cancelled, because those holds are no longer active.
- **A confirmed admission cannot be cancelled** (DELETE → 409). The fee is paid and admission is final; a withdrawal would need a refund process, which is out of scope.
- **Cancelling an already expired or cancelled hold returns 204** and changes nothing, so double-clicks and retries are harmless.
- **Confirming twice returns 200 CONFIRMED** both times, and the seat is counted only once.
- **Confirming an expired hold returns 410 Gone.** Confirming a cancelled hold returns 409. 410 is kept specifically for "time ran out" so the UI can say exactly that.
- **Acting on another student's hold returns 403.**
- **Anyone can read `GET /holds/{id}`** without the header, as shown in the spec.

## Input
- **`X-Student-Id`** must be exactly 17 digits; a missing or invalid header returns 400. It is stored as text so leading zeros are kept.
- **Class creation:** the name must be non-empty after trimming, and `totalSeats` must be a positive integer. Names need not be unique. Classes cannot be edited or deleted, since the spec has no endpoint for it.

## API behaviour
- **Error body:** `{ userResponse: { code, message, resourceId, errors[] }, devResponse: { exceptionType, message, traceId, stackTrace } }`. `devResponse` is always included. This makes problems easy to diagnose during evaluation; a real deployment would turn it off.
- **Ordering:** classes are sorted by name, and a student's holds are listed newest first.
- **`serverTime`** is added to responses (extra fields are allowed) so the countdown stays correct even when the user's clock is wrong.

## UI
- **Bangla/English toggle**, defaulting to Bangla, with the choice remembered. Error text is chosen from the error `code` on the frontend.
- **Seat counts and hold status refresh every 3 seconds.**
