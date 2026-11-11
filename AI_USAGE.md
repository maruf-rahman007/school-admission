# AI usage

## Tool

**Claude Code** (Anthropic's coding agent in the terminal) was the only AI tool.
- **Claude Opus** planned the work, wrote the acceptance criteria and wrote the code, tests and docs.
- **Claude Sonnet** reviewed each finished feature.

## How it was used

AI wrote most of the code, tests and documentation. I directed the work:
- I made every product and design decision. The AI had to ask, never assume, and each answer was written to a decision log. The reasoned ones are in [ASSUMPTIONS.md](ASSUMPTIONS.md).
- I approved every plan before any code was written.
- I ran and checked the app myself.
- I did every git commit, push and pull request. The agent was not allowed to change the repository history.

Each feature followed the same loop, with three separate AI roles so that no agent checked its own work:

1. **Plan:** the agent read the spec and the docs, then asked me about every open point, explaining the options. The answers went into the decision log. The result was a plan of small tasks, each about 200 lines with its tests.
2. **Acceptance criteria:** a *separate* agent, which never saw the planner's reasoning, wrote concrete criteria per task: functional, corner cases, technical, and required tests. Its open questions came back to me.
3. **Implement:** code, then tests. Backend tests ran against real PostgreSQL in Docker. Endpoints were smoke-tested with curl.
4. **Review:** a *fresh* reviewer re-ran the build, the tests and the smoke tests, and checked every criterion. Blockers were fixed before merging.

| Part | AI role |
|---|---|
| Architecture (row lock, query-time expiry, error format, ADRs) | Proposed the options; I chose |
| Backend (.NET, EF Core, raw SQL for holds, expiry sweeper) | Written by AI |
| Backend tests (contract, validation, concurrency, real-time expiry, two instances) | Written by AI |
| Frontend (React, TanStack Query, bn/en UI, countdown) | Written by AI; UI wording approved by me |
| Docker / compose, docs | Written by AI, checked by me |

## Where AI was wrong, and how it was caught

17 mistakes were logged during the project. The main ones:

| Mistake | Caught by |
|---|---|
| Planned a `serverTime` field on every response, which would have broken the array responses of `GET /classes` and `GET /me/holds` | Self-review of the architecture; fixed with an `X-Server-Time` header |
| The countdown read the time once at mount, before the server-time offset was known, so a fast client clock could wrongly show a held application as EXPIRED after a reload | Reviewer (blocker) |
| Pay/Cancel result toasts were attached in a way that TanStack Query drops if the page unmounts during the request | Reviewer (blocker) |
| A docs edit replaced the first matching table row, corrupting the configuration table in ARCHITECTURE.md | Reviewer (blocker); fixed in a follow-up commit |
| Assumed the wrong .NET test runner and package versions (xUnit v3 test platform; EF Core version conflict) | Failing builds/tests; fixed and pinned |
| Planned startup migrations without noticing the in-memory test hosts would then need (or wrongly migrate) a database | Criteria agent / wiring the tests; moved migrations into a hosted service that tests remove |
| Kept a hard-coded English label from a generated UI component (breaks the bn/en rule) | Reviewer |
| A UI scaffolding tool installed two unrelated npm packages | Noticed in the diff; removed |
| Added a stricter validation rule (CORS origins) without asking | Reviewer flagged it as an unapproved assumption; I then approved it |
| Twice claimed the spec could not be read, without checking the local copy | Criteria agent / self-noticed; criteria re-checked against the spec |

Lesson: the agent did not catch these mistakes in its own work. Separate criteria and review agents, tests against a real database, and the written decision log did. Only the docs table slipped into `main`, because I merged before that review had finished.
