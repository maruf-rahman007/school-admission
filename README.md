# School Admission System

First-come, first-served online admission with time-limited seat holds.
Backend: .NET 8 + PostgreSQL · Frontend: React (Vite) · Run with Docker Compose.

> 🚧 Work in progress. Run instructions, URLs and test commands will be added here.

## Bonus: two backends

Two backend processes on different ports can share one database without overbooking, because every rule is enforced inside PostgreSQL (row lock, unique index, database clock).

```sh
docker compose --profile multi up -d --build          # db, backend (:8080), backend2 (:8081), frontend (+ one check run in the background)
docker compose --profile multi run --rm multi-check   # 50 parallel applications, 25 to each backend
docker compose --profile multi down                   # stop (data is kept)
```

The check creates a new class `multi-check <timestamp>` with 5 seats and expects exactly 5 holds, 45 × `409 NO_SEATS_AVAILABLE`, and `heldSeats=5` reported by both backends. It prints `PASS` and exits 0, otherwise `FAIL: <reason>` and exits 1. Each run leaves that class (and its 5 holds, which expire after `HOLD_DURATION_SECONDS`) in the database.

The same rule is also covered by the automated test `MultiInstanceConcurrencyTests` (two app instances on one database), which runs with the other backend tests.

## Repository layout
```
backend/    .NET API (port 8080)
frontend/   React app
docs/       Architecture, standards, design
```

See also: [ASSUMPTIONS.md](ASSUMPTIONS.md) · [AI_USAGE.md](AI_USAGE.md)
