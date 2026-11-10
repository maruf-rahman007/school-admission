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
- **Server time** is sent so the countdown stays correct even when the user's clock is wrong: an `X-Server-Time` header on every response, and a `serverTime` field on object responses. Array responses (`GET /classes`, `GET /me/holds`) keep their exact shape and rely on the header. The server time comes from the backend's clock, which in Docker is the same host clock PostgreSQL uses; expiry itself is always decided by the database clock.
- **IDs** look like `c_<uuid>` (class) and `h_<uuid>` (hold). An ID that cannot be parsed returns 404, the same as an unknown ID.

## Concurrency, expiry and scaling
- **Seats are protected by a Postgres row lock** on the class while a hold is created, so held + confirmed can never exceed total seats. Redis was considered and rejected: it would be a second source of truth and extra infrastructure, and it isn't needed at this scale.
- **"One active hold per student"** is guaranteed by a partial unique index in the database, so even parallel requests from the same student cannot create two.
- **Expiry is decided at query time** using the database clock, so an expired hold stops counting immediately. A background job also marks expired holds as `EXPIRED` every 5 seconds.
- **Two or more backend processes are safe** because every rule is enforced inside PostgreSQL (row lock, unique index, database clock). Nothing is kept in backend memory. To try it, run a second backend container against the same `db` service (steps in README).

## Configuration
- **The frontend's backend URL** is the `VITE_API_BASE_URL` environment variable, passed to the frontend image as a build argument because Vite bakes it in at build time. After changing it, run `docker compose up --build`.
- **The database connection** is the `ConnectionStrings__Default` environment variable.
- **Backend tests** use a separate `admission_test` database, so running them never changes real data.
- **Invalid configuration stops the backend at startup.** If a setting is invalid (`HOLD_DURATION_SECONDS` or `EXPIRY_SWEEP_INTERVAL_SECONDS` not a positive whole number, `CORS_ALLOWED_ORIGINS` without a valid http(s) URL, or an empty connection string), the backend refuses to start: it logs every problem and exits with code 1, so a typo can't silently change behaviour. An empty variable counts as invalid; extra spaces and a trailing `/` in the CORS list are tolerated. A CORS entry with a path, query, user name/password or `#fragment` is rejected, because none of these is a valid browser origin.
- **Swagger UI is always on** (`/swagger`), including in Docker, so the API can be explored from a browser during evaluation. A real deployment would restrict it.

## UI
- **Bangla/English toggle**, defaulting to Bangla, with the choice remembered. Error text is chosen from the error `code` on the frontend.
- **Seat counts and hold status refresh every 3 seconds.**
- **Bangla digits** are shown in Bangla mode. The student-ID box accepts Bangla or English digits and sends English digits.
- **The student ID is asked for only when needed** (on Apply or on "My applications"), so anyone can browse classes, and the admin page needs no ID. The ID is remembered in the browser.
- **Only one application at a time:** while a student has an active application, Apply buttons are disabled with a link to it, matching the one-active-hold rule.
- **Paying and cancelling ask for confirmation first**, because a confirmed admission can't be undone.
- **The admin page is not protected**, because the spec has no login. In a real system it would be behind authentication.
- **`GET /holds/{id}` also returns `className`** (an extra field) so the application page can show the class name.
