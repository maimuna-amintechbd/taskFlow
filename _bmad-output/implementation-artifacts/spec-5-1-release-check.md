---
title: 'Story 5.1 — Release check: fresh clone, all acceptance checks, performance'
type: 'chore'
created: '2026-10-26'
status: 'in-review'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '52a02c50ad45a31d99497348de3544fa44c8513a'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-5-context.md'
  - '{project-root}/CLAUDE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release check NFR-6 ("Swagger lists every endpoint in the AD-9 table") fails today: `GET /api/health` is mapped with `MapHealthChecks`, which never appears in the OpenAPI document. A portfolio reviewer also has no single place that says how each release check (AC-1…AC-7) is proven.

**Approach:** Part A (this spec's code): expose `/api/health` in OpenAPI with an unchanged response, pin "every AD-9 path is documented" with a test, and add a README "Release checks" section. Part B (run by the orchestrator after Part A is committed, so it verifies exactly what is in git): a fresh clone following only the README, timed (SM-2); the AC-1…AC-7 checklist with results; the AC-4 restart repeated on the clone; 500 tasks under 1 s (NFR-7); keyboard-only UJ-1/UJ-2 (NFR-8). Part B results go in Implementation Notes.

## Boundaries & Constraints

**Always:**
- `/api/health` keeps its contract: `GET` → `200`, `Content-Type: text/plain`, body `Healthy` (existing `HealthTests` must pass unchanged). Use a minimal `app.MapGet("/api/health", …)` that calls the built-in `HealthCheckService.CheckHealthAsync()` (keep `AddHealthChecks()`), returns the report status as text (503 when not Healthy), with `.WithName("GetHealth")`, `.WithTags("Health")` and `.Produces<string>(200, "text/plain")`. A comment explains why it isn't `MapHealthChecks`.
- README "Release checks" section: a table AC-1…AC-7 → what it checks → how it is verified (named test classes/files or the manual step), matching the PRD's "Verified by" column. No dates or pass marks in the README (those live in the spec notes).

**Never:**
- No other behaviour change, no new dependencies, no committed load script or seed data (Part B's 500-task loop is throwaway).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Health unchanged | `GET /api/health` | 200, `text/plain`, `Healthy` | N/A |
| Every AD-9 path documented | `GET /openapi/v1.json` | `paths` contains `/api/health` (get), `/api/projects` (get, post), `/api/projects/{id}` (get, put, delete), `/api/tasks` (get, post), `/api/tasks/{id}` (get, put, delete), `/api/dashboard` (get) | N/A |
| README checklist | read `README.md` | a "Release checks" table covering AC-1…AC-7 with the verification method of each | N/A |

</frozen-after-approval>

## Code Map

- `backend/TaskFlow.Api/Program.cs` -- `AddHealthChecks()` registration and `app.MapHealthChecks("/api/health")` (near the end, before `MapControllers()`); replace the mapping only.
- `backend/TaskFlow.Api.Tests/HealthTests.cs` -- existing contract test; must stay green unchanged.
- `backend/TaskFlow.Api.Tests/OpenApiTests.cs` -- add `OpenApi_EveryApiEndpoint_IsDocumented` (the exact path → methods map from the matrix).
- `README.md` -- has "Run it locally", "Test and build", dependencies and the architecture overview; add "Release checks" after "Test and build". Test files to cite: `Tasks/CreateTaskTests.cs`, `Tasks/UpdateTaskTests.cs`, `Tasks/FilterTasksTests.cs`, `Persistence/TaskPersistenceTests.cs`, `Projects/CascadeAndForeignKeyTests.cs`, `Dashboard/DashboardTests.cs`.

## Tasks & Acceptance

**Execution:**
- [x] `backend/TaskFlow.Api/Program.cs` -- the health mapping as in Always.
- [x] `backend/TaskFlow.Api.Tests/OpenApiTests.cs` -- the documented-paths test.
- [x] `README.md` -- the Release checks section.

**Acceptance Criteria:**
- Given the running API in Development, when I open `/swagger`, then every endpoint from the AD-9 table is listed, including `GET /api/health`.

## Implementation Notes

- Part A review fixes: no-cache headers restored; 503 documented; comment corrected (Degraded→503 deliberate per spec); `HealthTests` additionally asserts `text/plain` (the unmodified test passed first, 174/174, proving the contract unchanged; the extra assertion only tightens it); documented-path set must equal AD-9 exactly; README test command/prerequisites link/NFR-6 note.
- Part A verification by the orchestrator (2026-10-26): `dotnet build` 0 warnings/0 errors; `dotnet test` all pass; `npm run build` ok; `npm run lint` clean.

## Spec Change Log

## Review Triage Log

| # | Layer | Finding | Verdict | Evidence / route |
|---|---|---|---|---|
| B1 / E5 / E7 | blind, edge | New health endpoint drops MapHealthChecks' no-cache headers ("unchanged response" claim) | low | Direct header restore → **patch**. |
| B3 / E3 | blind, edge | 503 not declared in OpenAPI | low | Direct `.Produces` → **patch**. |
| B7 | blind | Program.cs comment inaccurate about why MapHealthChecks is not documented | low | Learning codebase; comment fix → **patch** (also notes Degraded→503 is deliberate). |
| B5 | blind | HealthTests doesn't assert `text/plain` | low | Direct assertion → **patch**. |
| B6 / E4 | blind, edge | Documented-paths test checks one direction only | low | AD-9 says exactly the table; direct set-equality assertion → **patch**. |
| B8 / B9 | blind | README test command imprecise; AC-7 row misses Prerequisites; NFR-6 test unmentioned | low | Direct doc fixes → **patch**. |
| B2 / E1 | blind, edge | Degraded now 503 (MapHealthChecks default 200) | false | Frozen spec: "503 when not Healthy"; no health checks are registered, so only Healthy is reachable. Kept; documented in the comment. |
| B4 | blind | 503 branch untested | low | Unreachable without registering a failing check; the spec contract is the Healthy path. Rejected. |
| E6 | edge | HEAD /api/health now 405 | low | No caller uses HEAD; spec asks for MapGet. Rejected. |
| E2 | edge | Client abort logs an unhandled cancellation | low | Logging noise only. Rejected. |
| VG-o | verify | Degraded/no-cache behaviour differences | low | Same as B1/B2 above. |

## Verification

**Commands:**
- `cd backend && dotnet build` -- expected: 0 errors, 0 warnings
- `cd backend && dotnet test` -- expected: all pass (173 existing + new)
- `cd frontend && npm run build && npm run lint` -- expected: success (no frontend changes)
