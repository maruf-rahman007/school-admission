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
- **Class creation:** the name must be non-empty after trimming, and `totalSeats` must be a positive integer sent as a JSON number (`"40"` as a string is rejected as malformed). Unknown extra fields in the body are ignored. Names need not be unique. Classes cannot be edited or deleted, since the spec has no endpoint for it.

## API behaviour
- **Error body:** `{ userResponse: { code, message, resourceId, errors[] }, devResponse: { exceptionType, message, traceId, stackTrace } }`. `devResponse` is always included. This makes problems easy to diagnose during evaluation; a real deployment would turn it off.
- **Ordering:** classes are sorted by name, number-aware and ignoring case ("Class 6" before "Class 10"), so admins see classes in the order they expect; classes with the same name are listed oldest first. A student's holds are listed newest first.
- **Server time** is sent so the countdown stays correct even when the user's clock is wrong: an `X-Server-Time` header on every response, and a `serverTime` field on object responses. Array responses (`GET /classes`, `GET /me/holds`) keep their exact shape and rely on the header. The server time comes from the backend's clock, which in Docker is the same host clock PostgreSQL uses; expiry itself is always decided by the database clock.
- **IDs** look like `c_<uuid>` (class) and `h_<uuid>` (hold). An ID that cannot be parsed returns 404, the same as an unknown ID.
- **Confirm returns the full application** (the same fields as `GET /holds/{id}`), so the client can show the new state without another request.
- **Check order on confirm and cancel:** a missing or invalid student ID (400) is reported first, then an unknown hold (404), then someone else's hold (403), then the status rules. This is the same order as Apply.

## Concurrency, expiry and scaling
- **Seats are protected by a Postgres row lock** on the class while a hold is created, so held + confirmed can never exceed total seats. Redis was considered and rejected: it would be a second source of truth and extra infrastructure, and it isn't needed at this scale.
- **"One active hold per student"** is guaranteed by a partial unique index in the database, so even parallel requests from the same student cannot create two.
- **Expiry is decided at query time** using the database clock, so an expired hold stops counting immediately. A background job also marks expired holds as `EXPIRED` every 5 seconds. If that job hits a database error, it logs a warning and tries again on the next run instead of stopping the backend, because expiry is already correct without it.
- **Two or more backend processes are safe** because every rule is enforced inside PostgreSQL (row lock, unique index, database clock). Nothing is kept in backend memory. This is proven twice: an automated test runs two app instances against one database, and `docker compose --profile multi` starts a second backend on port 8081 with a check that sends 50 parallel applications to both ports (README, "Bonus: two backends").

## Configuration
- **The frontend's backend URL** is the `VITE_API_BASE_URL` environment variable, passed to the frontend image as a build argument because Vite bakes it in at build time. After changing it, run `docker compose up --build`.
- **The database connection** is the `ConnectionStrings__Default` environment variable.
- **Backend tests** use a separate `admission_test` database, so running them never changes real data. It is recreated fresh at the start of every test run, and the test setup refuses to run against a database with any other name.
- **Invalid configuration stops the backend at startup.** If a setting is invalid (`HOLD_DURATION_SECONDS` or `EXPIRY_SWEEP_INTERVAL_SECONDS` not a positive whole number, `CORS_ALLOWED_ORIGINS` without a valid http(s) URL, or an empty connection string), the backend refuses to start: it logs every problem and exits with code 1, so a typo can't silently change behaviour. An empty variable counts as invalid; extra spaces and a trailing `/` in the CORS list are tolerated. A CORS entry with a path, query, user name/password or `#fragment` is rejected, because none of these is a valid browser origin.
- **A failed database migration stops the backend at startup** (logged, exit code 1), the same as invalid configuration, so it never serves requests against a half-migrated schema. Docker Compose restarts it automatically, which covers a short database hiccup.
- **Swagger UI is always on** (`/swagger`), including in Docker, so the API can be explored from a browser during evaluation. A real deployment would restrict it.

## UI
- **Bangla/English toggle**, defaulting to Bangla, with the choice remembered. Error text is chosen from the error `code` on the frontend.
- **Seat counts and hold status refresh every 3 seconds.**
- **Bangla digits** are shown in Bangla mode. The student-ID box accepts Bangla or English digits and sends English digits.
- **The student ID box is in the page header** (shown with a Change button; entered in a small dialog). **It is asked for only when needed** (on Apply, Pay, Cancel or "My applications"), so anyone can browse classes, and the admin page needs no ID. The ID is remembered in the browser.
- **"My applications" shows every application**, newest first, including expired and cancelled ones, so a parent can see what happened to each; each one links to its page.
- **When the countdown reaches zero the application shows EXPIRED at once** and the Pay button disappears, without waiting for the next refresh; the server's own check (database clock) still decides.
- **Only one application at a time:** while a student has an active application, Apply buttons are disabled with a link to it, matching the one-active-hold rule.
- **Paying and cancelling ask for confirmation first**, because a confirmed admission can't be undone.
- **The admin page is not protected**, because the spec has no login. In a real system it would be behind authentication.
- **`GET /holds/{id}` also returns `className`** (an extra field) so the application page can show the class name.
