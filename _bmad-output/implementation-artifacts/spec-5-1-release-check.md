---
title: 'Story 5.1 — Release check: fresh clone, all acceptance checks, performance'
type: 'chore'
created: '2026-10-26'
status: 'done'
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

### Part B — release verification (orchestrator, 2026-10-26, on commit `baf720a`)

**Method:** `git clone` of the local repo (Days 3–7 are not pushed yet) into an empty scratch folder, then only the README's commands. Package caches (npm, NuGet) were already warm on this machine, so downloads were near-instant; a cold machine adds the package download time.

| Check | Verified by | Result |
|---|---|---|
| SM-2 / AC-7 fresh clone ≤ 10 min | clone → `dotnet run --project TaskFlow.Api` + `npm install && npm run dev` → keyboard UJ-1/UJ-2 in the UI → AC-4 restart → `(cd backend && dotnet test)` → `(cd frontend && npm run build)` | **PASS** — apps up 7 s after clone, whole flow 2 min 47 s; `taskflow.db` created automatically; 174/174 tests; build ok. Finding: README still said the Dashboard shows "API connected" (stale since Story 4.2) → fixed in this commit. |
| AC-1 valid title | `CreateTaskTests`, `UpdateTaskTests` (174/174 on the clone) | **PASS** |
| AC-2 valid project | `CreateTaskTests`, `UpdateTaskTests`, FK test in `CascadeAndForeignKeyTests` | **PASS** |
| AC-3 status/priority (body + filters) | `CreateTaskTests`, `UpdateTaskTests`, `FilterTasksTests` | **PASS** |
| AC-4 persistence after restart | `TaskPersistenceTests` + manual on the clone: edited "Plan the week" → "(edited)", Done in the browser; API stopped and restarted; GET returned the edited title, Done, due 2026-10-30 | **PASS** |
| AC-5 cascade | `CascadeAndForeignKeyTests` (AC-5 test) + UJ-2 in the UI: `Delete project "Portfolio site" and its 3 tasks?` → dashboard 0/0/0 | **PASS** |
| AC-6 dashboard totals | `DashboardTests` + UI: 3/0/3 after UJ-1, 0/0/0 after UJ-2, 501/175/326 with the load data | **PASS** |
| NFR-6 Swagger lists every endpoint | `OpenApi_EveryApiEndpoint_IsDocumented` + live `/openapi/v1.json` on the clone: health get; projects get/post; projects/{id} get/put/delete; tasks get/post; tasks/{id} get/put/delete; dashboard get; `/swagger` 200 | **PASS** |
| NFR-7 < 1 s with 500 tasks | 500 tasks via a throwaway Python loop (not committed). Production build (`vite preview`): Tasks page 95–150 ms (API 9–11 ms), filtered 48–72 ms (API 6–10 ms), Dashboard 40–46 ms (API 6–8 ms). Dev server: 543–1307 ms page-visible (Vite compiling modules on demand + React StrictMode double requests; API calls 10–41 ms) | **PASS** (production); dev-mode overhead noted |
| NFR-8 keyboard, Chrome | Keyboard-only script (Tab/Enter/arrows/typed keys, no clicks): UJ-1 create project, 3 tasks (priorities via arrow keys), filter Todo+High, set InProgress; UJ-2 delete project + confirm. 80 Tab presses; 0 unlabelled inputs. Due date typed digit by digit → `2026-10-30` saved | **PASS** |
| NFR-8 Firefox | Firefox 157 headless renders the app shell (nav, layout, styles) on Dashboard, filtered Tasks and Project pages | **PARTIAL** — rendering only; keyboard walkthrough in Firefox left as a manual check for the user |
| NFR-8 Edge | not installed on this machine | **NOT RUN** |

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
