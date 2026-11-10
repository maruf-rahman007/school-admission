# Engineering Standards

These are the rules every change must follow. Reviews check against this file, and its last section is the Definition of Done.

## 1. Stack

| Area | Choice |
|---|---|
| Backend | .NET 10, ASP.NET Core **Controllers** |
| Data | PostgreSQL. **EF Core** for the schema, migrations and simple CRUD. **Raw SQL** for concurrency-critical paths (creating and confirming holds) |
| Backend tests | **xUnit**, run inside Docker against a compose Postgres: `docker compose run --rm backend-tests` |
| Frontend | React 18+, Vite, **TypeScript (strict)** |
| UI | **Tailwind CSS + shadcn/ui** |
| Data fetching | **TanStack Query + axios** |
| Frontend tests | Decided later (bonus) |
| Lint/format | No enforced tooling. Follow `.editorconfig` and the conventions below |
| Backend architecture | **Clean Architecture** (4 projects) + **CQRS** with plain handler classes, **no MediatR** |
| Validation | **FluentValidation** |
| Frontend structure | **Modular**: `src/modules/*` with a public `index.ts` each |

## 1a. Backend structure (Clean Architecture + CQRS)
```
backend/
  SchoolAdmission.sln
  src/
    SchoolAdmission.Domain/          entities (SchoolClass, Hold), HoldStatus, domain rules, domain exceptions
    SchoolAdmission.Application/     Commands/ + Queries/ (one folder per use case: command/query, handler, validator, DTOs),
                                     abstractions (repository interfaces)
    SchoolAdmission.Infrastructure/  EF Core DbContext, configurations, migrations, repositories (raw SQL for holds),
                                     expiry background service
    SchoolAdmission.Api/             Controllers, error-handling middleware, DI composition, config
  tests/
    SchoolAdmission.Tests/           xUnit: domain unit tests + integration/concurrency tests vs Postgres
```
**Dependency rule:** Domain depends on nothing. Application depends on Domain. Infrastructure depends on Application and Domain. Api depends on all three, only to wire up DI. Never reference inward-pointing projects in the opposite direction (e.g. Domain must never use EF Core).

**CQRS without MediatR:**
- **Commands** change state: `CreateClass`, `CreateHold`, `ConfirmHold`, `CancelHold`.
- **Queries** only read: `GetClasses`, `GetClassById`, `GetHoldById`, `GetMyHolds`.
- Each use case gets its own folder, e.g. `Application/Holds/Commands/CreateHold/` containing `CreateHoldCommand`, `CreateHoldHandler`, `CreateHoldValidator`, and the result DTO.
- Handlers are plain classes with `Task<TResult> Handle(TCommand cmd, CancellationToken ct)`. Register them in DI and inject them straight into controllers. Do not add a mediator or dispatcher.
- Validation: the handler runs its FluentValidation validator first, or an action filter runs it before the handler is called. A failed validation becomes a 400 with `errors[]`.
- Query handlers may read with projections (`AsNoTracking`, `Select`) or SQL and need not load full entities.
- **Concurrency-critical writes** (create and confirm hold) are implemented in Infrastructure repositories as raw SQL inside a transaction, as specified in `docs/ARCHITECTURE.md`.

## 1b. Frontend structure (modular)
```
frontend/src/
  app/                 App root, providers (QueryClient, i18n), router, error boundary
  modules/
    classes/           create-class form, class list, seat counts
    holds/             application view (countdown, pay, cancel), my applications
    student/           student ID bar + persisted student ID
      components/  hooks/  api/  types.ts  index.ts   ← each module follows this layout
  shared/
    api/               axios client, ApiError, runtime config
    i18n/              bn.ts, en.ts, language toggle
    ui/                shadcn/ui components
    lib/               utils (time/countdown, formatting)
```
- **A module's public API is its `index.ts`.** Other modules import only from `@/modules/<name>`, never from deep paths.
- Modules may import from `shared/`. `shared/` must never import from `modules/`.
- This rule is checked in review (there is no lint tooling).

## 2. C# conventions
- `<Nullable>enable</Nullable>` and `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in every project.
- Use `async`/`await` all the way down. Never call `.Result` or `.Wait()`. Pass a `CancellationToken` through to the database.
- Keep controllers thin: map HTTP to a command or query, call its handler, and map the result. Business rules belong in Domain and Application handlers. SQL and EF code stays in Infrastructure.
- **Request and response DTOs are separate from EF entities.** Never return an entity from an API.
- DTO property names must serialize exactly as the contract expects (camelCase, e.g. `totalSeats`, `holdId`).
- Naming: `PascalCase` for types and members, `_camelCase` for private fields, and file name = type name.
- Business-rule failures throw typed exceptions (e.g. `NoSeatsAvailableException`). A single global handler turns them into the error body (§5).
- All timestamps are `DateTimeOffset` in UTC and serialize as ISO-8601 with `Z`.
- **Expiry decisions use the database clock (`now()`) only.** Never compare `expires_at` with `DateTime.UtcNow` in C#. There is no `IClock` abstraction (ADR-0002).
- Configuration comes only from environment variables (and `appsettings` defaults). Never hard-code connection strings.

## 3. TypeScript / React conventions
- Keep `strict: true`. Do not use `any`; use `unknown` and narrow it when the type is genuinely unknown.
- Function components and hooks only. Name component files in `PascalCase.tsx` and hooks `useXxx.ts`.
- **All HTTP goes through one axios client** that sets the base URL from runtime config, adds `X-Student-Id`, and maps errors to a typed `ApiError`.
- **All server state goes through TanStack Query.** Polling uses `refetchInterval: 3000`. Do not use `useEffect` + `fetch`.
- API types in each module's `types.ts` mirror the contract exactly.
- **No hard-coded user-facing text.** Every string comes from the bn/en dictionaries. Error text is looked up by the error `code`.
- Style with Tailwind utility classes and shadcn/ui components. Avoid custom CSS files.
- Never show a blank page. Wrap the app in an error boundary and give every query a loading state and an error state.

## 4. API rules
- **The contract is frozen.** You may add fields to responses. Never remove or rename a field, and never change a status code.
- All requests and responses are JSON. Every response carries an `X-Server-Time` header, and object (non-array) responses also carry a `serverTime` field. Never wrap an array response in an object.
- IDs are prefixed UUIDs (`c_…`, `h_…`). An unparseable ID returns 404.
- The student is identified by the `X-Student-Id` header, which must match `^[0-9]{17}$`. Otherwise respond 400.

### Status codes

| Situation | Code |
|---|---|
| Created a class or hold | 201 |
| Read OK, or confirm OK (including a repeat confirm) | 200 |
| Cancel OK (including an already expired or cancelled hold) | 204 |
| Invalid input, missing or invalid `X-Student-Id`, malformed JSON | 400 |
| Hold belongs to another student | 403 |
| Class or hold not found | 404 |
| No seats; student already has an active hold anywhere; cancel a CONFIRMED hold; confirm a CANCELLED hold | 409 |
| Confirm an EXPIRED hold | 410 |
| Unexpected server fault (must never happen because of client input) | 500 |

## 5. Error body
Every non-2xx response has this shape:
```json
{
  "userResponse": {
    "code": "NO_SEATS_AVAILABLE",
    "message": "No seats are available in this class.",
    "resourceId": "…",
    "errors": [{ "field": "totalSeats", "code": "OUT_OF_RANGE", "message": "…" }]
  },
  "devResponse": {
    "exceptionType": "NoSeatsAvailableException",
    "message": "…",
    "traceId": "…",
    "stackTrace": "…"
  }
}
```
- `devResponse` is **always** included (decision in ASSUMPTIONS.md).
- `code` values are `UPPER_SNAKE_CASE`, stable, and listed in `docs/ARCHITECTURE.md`. The frontend relies on them.
- `userResponse.message` is in English. The frontend translates by `code`.
- Model-binding and validation errors also use this shape. Turn off the default ProblemDetails response.

## 6. Testing
- The backend has at least 3 automated tests, and they must include:
  - a **concurrency test**: N seats and many more parallel hold requests; exactly N succeed, the rest get 409, and the DB counts are consistent;
  - expiry, idempotent confirm, the one-active-hold rule, and validation (400 rather than 500).
- Tests use a real Postgres. Never mock the database for business-rule tests.
- Name tests `Method_Scenario_ExpectedResult` (e.g. `CreateHold_WhenClassFull_Returns409`).
- Tests must be independent: each one creates its own class and student IDs.
- Any change to concurrency or expiry code must come with a test that exercises it.

## 7. Git
- **The user runs all git commands** (branch, commit, push, PR). Agents never do.
- Branches are named `feature/T-xxx-slug`, cut from `main` and merged back by PR.
- Commit messages follow Conventional Commits: `feat(backend): …`, `fix(frontend): …`, `docs: …`, `test: …`, `chore: …`.
- Keep commits small, one planned task each.
- Never commit `*.pdf`, `CLAUDE.md`, `.claude/`, `tickets/` or `.env`.

## 8. Definition of Done (a task is done when every box is checked)
- [ ] The task's acceptance criteria are met
- [ ] The backend builds with **zero warnings**, and the frontend passes `tsc` with no errors
- [ ] Tests pass (`docker compose run --rm backend-tests`), and new behaviour has tests
- [ ] The API contract is unchanged: same field names and status codes as §4
- [ ] Bad input produces a 4xx with the §5 error body, never a 500
- [ ] Every new UI string exists in both the bn and en dictionaries
- [ ] Every assumption is backed by a `DECISIONS.md` entry. Anything new was asked, not guessed
- [ ] No secrets, PDF or agent files are in the diff
- [ ] README, ASSUMPTIONS and AI_USAGE are updated if the change affects them
