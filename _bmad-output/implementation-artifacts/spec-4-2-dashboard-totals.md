---
title: 'Story 4.2 — Dashboard totals'
type: 'feature'
created: '2026-10-26'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'ee560905da203b03ebfe221e24ee37957d213b17'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-4-context.md'
  - '{project-root}/CLAUDE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The Dashboard still shows the walking-skeleton "API connected" text, so the user can't see how much work is left, and AC-6 (dashboard totals reflect saved data) is unproven.

**Approach:** Add `GET /api/dashboard` → `{ total, completed, pending }`, counted from the database on every request by a small `DashboardService`. Replace the Dashboard page's health check with three labelled numbers.

## Boundaries & Constraints

**Always:**
- Total = all tasks; Completed = `Done`; Pending = `Todo` + `InProgress` (so Total = Completed + Pending). The counts come from the database with async EF queries on every request; nothing is cached or stored.
- `DashboardDto` is a record in `Dtos/`; `DashboardService` is concrete, `AddScoped`; `DashboardController` is HTTP-only (`Get`, 200, `ProducesResponseType<DashboardDto>`).
- The page fetches on every open or reload, shows the three numbers as labelled values (e.g. a `<dl>` with `<dt>`/`<dd>`), and shows `ErrorBanner` (via `messageOf`) instead of numbers on failure. It uses the shared `messageOf`/`FALLBACK_MESSAGE` from `client.ts` instead of its inline copy.

**Never:**
- No per-project counts, charts, overdue counts or live updates (PRD §6); no new dependencies.
- Don't remove `GET /api/health` or `frontend/src/api/health.ts`; only the Dashboard stops using it.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Empty | no tasks (projects may exist) | 200 `{ total: 0, completed: 0, pending: 0 }` | N/A |
| Mixed | Todo, InProgress, Done, Done (in two projects) | `{ 4, 2, 2 }` | N/A |
| After create (AC-6) | add a Todo task | total +1, pending +1 | N/A |
| After edit to Done (AC-6) | PUT an InProgress task to Done | completed +1, pending −1, total same | N/A |
| After task delete (AC-6) | DELETE a Done task | total −1, completed −1 | N/A |
| After project delete (AC-6) | DELETE a project with 2 tasks | total −2, the two counts drop by those tasks' statuses | N/A |
| Page OK | open `/` | "Total", "Completed", "Pending" each with its number | N/A |
| Page reload | create a task elsewhere, reload `/` | the new numbers | N/A |
| Page API down | API stopped, open `/` | `ErrorBanner` "Cannot reach the server. Is the API running?", no numbers | N/A |

</frozen-after-approval>

## Code Map

- `backend/TaskFlow.Api/Program.cs` -- service registrations next to `AddScoped<TaskService>()`; add `AddScoped<DashboardService>()`.
- `backend/TaskFlow.Api/Controllers/ProjectsController.cs` -- the controller style to copy (attribute route, primary-constructor service, `ProducesResponseType`).
- `backend/TaskFlow.Api.Tests/TestInfrastructure/HttpClientExtensions.cs` -- `CreateProjectAsync`, `CreateTaskAsync` for test setup.
- `backend/TaskFlow.Api.Tests/OpenApiTests.cs` -- add `GET /api/dashboard` documents `200`.
- `frontend/src/pages/DashboardPage.tsx` -- currently calls `getHealth()` and shows "API connected"; replace with the dashboard fetch and the numbers.
- `frontend/src/api/client.ts` -- `request`, `messageOf`.

## Tasks & Acceptance

**Execution:**
- [x] `backend/TaskFlow.Api/Dtos/DashboardDto.cs`, `Services/DashboardService.cs`, `Controllers/DashboardController.cs`, `Program.cs` -- the endpoint.
- [x] `backend/TaskFlow.Api.Tests/Dashboard/DashboardTests.cs` -- one test per API matrix row (the AC-6 rows as separate tests).
- [x] `backend/TaskFlow.Api.Tests/OpenApiTests.cs` -- the dashboard response.
- [x] `frontend/src/api/types.ts` (`DashboardDto`), `api/dashboard.ts` (`getDashboard`), `pages/DashboardPage.tsx`, `index.css` -- the page rows.

**Acceptance Criteria:**
- Given `DashboardController`, when I read it, then it holds no counting logic and no DbContext.

## Implementation Notes

- Review fixes: Total = sum of all grouped counts (robust to new statuses); comment corrected; mixed fixture 1 Todo / 3 InProgress / 2 Done → 6/2/4 (detects swapped buckets); camelCase test parses JSON.
- Verification by the orchestrator (2026-10-26): `dotnet build` 0 warnings/0 errors; `dotnet test` 173/173; `npm run build` ok; `npm run lint` clean. Headless Chrome on a scratch DB: empty → Total 0 / Completed 0 / Pending 0; after Todo + InProgress + Done → 3/1/2; after editing the InProgress task to Done and reloading → 3/2/1; API stopped → "Cannot reach the server. Is the API running?" with no numbers.

## Spec Change Log

## Review Triage Log

| # | Layer | Finding | Verdict | Evidence / route |
|---|---|---|---|---|
| B1 / E1 | blind, edge | Total = completed + pending; a future status would vanish from Total | low | Contradicts "Total = all tasks" by construction; direct fix (`Values.Sum()`) → **patch**. |
| B2 | blind | Comment gives the wrong reason for Total = Completed + Pending | low | Misleading in a learning codebase; comment fix → **patch**. |
| B3 | blind | Mixed fixture (2/2) can't detect a swapped bucket | medium | Swapping Done/InProgress mapping still yields 4/2/2. Direct fixture change → **patch**. |
| B7 | blind | Exact-string JSON test is fragile | low | Direct `JsonDocument` rewrite → **patch**. |
| VG1 | verify | Dashboard page rendering/error state only checked by hand | medium | No FE test runner by architecture decision → **defer** (same decision as earlier stories). |
| E2 | edge | Unparseable status text in the DB → 500 | low | Only via manual DB edits; status is always written through the enum converter. Rejected. |
| E3 | edge | `frontend/src/api/health.ts` now unused | low | Spec (frozen) keeps it deliberately; `/api/health` stays for README/tests. Rejected. |
| B4 | blind | Bare zeros on an empty dashboard, no hint | low | MVP shows counts only (FR-10 "all three counts show 0"); hint is polish. Rejected. |
| B5 | blind | "Loading totals…" not announced (no aria-live) | low | Accessibility polish (Story 5.2). Rejected. |
| B6 | blind | OpenAPI test doesn't assert integer types / required | low | Same rejection as 3.3 B8. Rejected. |
| B8 | blind | Verification results not recorded; spec/sprint files not in diff | false | Orchestrator ran build/test/lint (173/173) before review; results recorded at step 5; tracking files are excluded from review diffs on purpose. |

## Verification

**Commands:**
- `cd backend && dotnet build` -- expected: 0 errors, 0 warnings
- `cd backend && dotnet test` -- expected: all pass
- `cd frontend && npm run build && npm run lint` -- expected: success, no warnings

**Manual checks:**
- Browser on a scratch DB: the page rows of the matrix.
