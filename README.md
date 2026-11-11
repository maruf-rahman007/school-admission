# School Admission System

First-come, first-served online admission with time-limited seat holds. A school admin creates classes (one per shift, each with its own seat count); parents apply for a seat, which is held for a limited time until the admission fee is paid.

**Stack:** .NET 10 (ASP.NET Core, EF Core) · PostgreSQL 17 · React 19 + TypeScript (Vite, TanStack Query, Tailwind/shadcn) · Docker Compose.

## Run the app

Only Docker is needed.

```sh
docker compose up --build
```

This starts PostgreSQL, the backend and the frontend. The first build takes a few minutes.

| What | URL |
|---|---|
| **Frontend** (the app) | <http://localhost:3000> |
| Backend API | <http://localhost:8080> |
| API docs (Swagger UI) | <http://localhost:8080/swagger> |
| Health check | <http://localhost:8080/health> |

The UI is in Bangla by default; switch to English with **বাংলা | EN** in the header. Enter a 17-digit birth registration number as the student ID when asked (on Apply or on "My applications").

**Stop:** `docker compose down`. Data is kept in the `pgdata` volume and is still there after the next `docker compose up` (`docker compose down -v` deletes it).

## Configuration

Every setting is an environment variable with a default, so nothing has to be set. To change one, export it or put it in a `.env` file next to `docker-compose.yml` (see [`.env.example`](.env.example)).

| Variable | Default | Meaning |
|---|---|---|
| `HOLD_DURATION_SECONDS` | `600` | How long a seat hold lasts before it expires |
| `EXPIRY_SWEEP_INTERVAL_SECONDS` | `5` | How often a background job marks overdue holds `EXPIRED` (expiry itself is always decided at query time) |
| `ConnectionStrings__Default` | `Host=db;Port=5432;Database=admission;Username=admission;Password=admission` | Backend database connection |
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | `admission` / `admission` / `admission` | Database server credentials (must match the connection string) |
| `CORS_ALLOWED_ORIGINS` | `http://localhost:3000` | Frontend origins allowed to call the backend (comma-separated) |
| `VITE_API_BASE_URL` | `http://localhost:8080` | Backend URL used by the frontend |

**Changing `VITE_API_BASE_URL` needs a rebuild:** Vite bakes it into the JavaScript at build time, so run `docker compose up --build` after changing it. If the frontend's URL or port changes, add its origin to `CORS_ALLOWED_ORIGINS`.

Example — 30-second holds to watch expiry quickly:

```sh
HOLD_DURATION_SECONDS=30 docker compose up -d --build
```

An invalid value (e.g. `HOLD_DURATION_SECONDS=abc`) stops the backend at startup with a clear log message instead of silently using a default.

## Run the tests

**Backend** (xUnit, against a real PostgreSQL in Docker — no .NET SDK needed on the host):

```sh
docker compose run --rm backend-tests
```

The tests use their own database `admission_test` on the same server, recreated on every run, so they never touch the app's data. They cover the API contract, validation (400 instead of 500), the hold rules, real-time expiry and **concurrency**: 50 parallel applications for a 5-seat class give exactly 5 holds.

**Frontend:** there are no automated frontend tests (the optional bonus was not done). `npm run build` type-checks the code (TypeScript strict) and runs inside the frontend Docker build (`docker compose build frontend`), so no local Node is needed.

## Bonus: two backends

Two backend processes on different ports can share one database without overbooking, because every rule is enforced inside PostgreSQL (row lock, unique index, database clock).

```sh
docker compose --profile multi up -d --build          # db, backend (:8080), backend2 (:8081), frontend (+ one check run in the background)
docker compose --profile multi run --rm multi-check   # 50 parallel applications, 25 to each backend
docker compose --profile multi down                   # stop (data is kept)
```

The check creates a new class `multi-check <timestamp>` with 5 seats and expects exactly 5 holds, 45 × `409 NO_SEATS_AVAILABLE`, and `heldSeats=5` reported by both backends. It prints `PASS` and exits 0, otherwise `FAIL: <reason>` and exits 1. Each run — including the automatic one started by `--profile multi up` — leaves that class (and its 5 holds, which expire after `HOLD_DURATION_SECONDS`) in the database.

The same rule is also covered by the automated test `MultiInstanceConcurrencyTests` (two app instances on one database), which runs with the other backend tests.

## Repository layout

```
backend/     .NET API (Clean Architecture: Domain, Application, Infrastructure, Api) + xUnit tests
frontend/    React app (src/modules: classes, holds, student; src/shared: api, i18n, ui)
docs/        ARCHITECTURE.md, STANDARDS.md, UI_DESIGN.md, adr/ (architecture decisions)
scripts/     multi-instance-check.sh (bonus check)
docker-compose.yml
```

- [ASSUMPTIONS.md](ASSUMPTIONS.md) — what the spec leaves open and what was decided, with reasons.
- [AI_USAGE.md](AI_USAGE.md) — how AI was used, and where it was wrong.
- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — how the system works (concurrency, expiry, errors, API details); [docs/adr/](docs/adr/) — key decisions.
- [docs/UI_DESIGN.md](docs/UI_DESIGN.md) — screens, states and texts; [docs/STANDARDS.md](docs/STANDARDS.md) — coding rules.
