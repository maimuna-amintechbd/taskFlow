---
title: 'Story 4.1 — Filter tasks by status and priority'
type: 'feature'
created: '2026-10-26'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'bbc6c5b3f90fd89fc3a2cb4e2154b0bf540d507e'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-4-context.md'
  - '{project-root}/CLAUDE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The user can't narrow the task list to decide what to work on next, and AC-3's query-string half (invalid status/priority filters rejected) is unproven.

**Approach:** Add `status` and `priority` to `GET /api/tasks`, parsed by the existing `EnumNames.TryParse` (AND-combined, also with `projectId`, all errors in one pass). Add a `TaskFilters` component whose state lives in the URL, used on the Tasks page and in the project task view, with a filter-specific empty message.

## Boundaries & Constraints

**Always:**
- `TaskListQuery.Status`/`Priority`: `string?` with `[DisplayFormat(ConvertEmptyStringToNull = false)]`; omitted = All; present-but-invalid (including `""`) → 400 with the same messages as the body ("Status must be one of: Todo, InProgress, Done." / "Priority must be one of: Low, Medium, High."). Validate every parameter before returning, so several bad ones come back together.
- The frontend filter state lives only in the URL query (`useSearchParams`); "All" removes the parameter; changing a filter pushes a history entry (the back button works). The page passes the API only values that exactly match `TASK_STATUSES`/`TASK_PRIORITIES`; anything else in the URL is treated as All.
- `TaskFilters`: two labelled native `<select>`s (All + options from the const arrays), presentational, props in, `onChange` out.
- Empty states: no filter active → "No tasks yet."; any filter active and no match → "No tasks match these filters."

**Never:**
- No free-text search, sorting options, multi-select or pagination (PRD §6); no new dependencies; no client-side filtering of an unfiltered list (the API filters).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Both filters | mixed tasks in two projects; `?status=todo&priority=HIGH` | only Todo+High tasks, newest first | N/A |
| One filter | `?status=Done` / `?priority=Low` | only matching tasks | N/A |
| With project | `?projectId=P&status=InProgress` | only P's InProgress tasks | N/A |
| Omitted | no status/priority | all tasks (existing behaviour) | N/A |
| Invalid (AC-3) | `?status=7`, `?status=2`, `?status=`, `?status=Hgh`, `?priority=7`, `?priority=` | 400 `errors.status` / `errors.priority` with the body's "must be one of" message | N/A |
| Several invalid | `?status=7&priority=7&projectId=abc` | one 400 with `status`, `priority`, `projectId` | N/A |
| UI choose | Tasks page: Status = Todo, Priority = High | URL `/tasks?status=Todo&priority=High`; only matches listed | load failure → banner |
| UI All | set Status back to All | `status` removed from the URL; list re-fetched | N/A |
| UI reload / back | reload; then browser back | filters and list survive the reload; back restores the previous filter | N/A |
| UI no match | filters with no matching task | "No tasks match these filters." | N/A |
| UI project view | project page, Status = Done | URL `/projects/P?status=Done`; that project's Done tasks only; no match → filter message | N/A |
| UI junk URL | `/tasks?status=bogus` | treated as All (select shows All, full list) | N/A |

</frozen-after-approval>

## Code Map

- `backend/TaskFlow.Api/Dtos/TaskListQuery.cs` -- only `ProjectId` today; add `Status`, `Priority` the same way.
- `backend/TaskFlow.Api/Services/TaskService.cs:42-70` -- `ListAsync` validates `ProjectId` and returns early; restructure so every parameter is validated into one error dictionary before returning, then apply `Where` filters; keep the ordering.
- `backend/TaskFlow.Api/Services/EnumNames.cs` -- reuse `TryParse` and `List<T>()` for the messages.
- `backend/TaskFlow.Api.Tests/Tasks/ListTasksTests.cs` -- the existing list tests; add filter tests next to them or in `FilterTasksTests.cs`.
- `frontend/src/api/tasks.ts` -- `TaskListFilters` / `listTasks`; add `status?`, `priority?`.
- `frontend/src/components/TaskTable.tsx:18` -- the empty message is hard-coded; add an `emptyMessage` prop (default "No tasks yet.").
- `frontend/src/pages/TasksPage.tsx` (`loadTasks` `:22`, mount effect `:30`) and `frontend/src/pages/ProjectPage.tsx` (`loadTasks` `:67`, tasks effect `:91`) -- read the filters from `useSearchParams`, include them in `listTasks` and in the effect dependencies, render `TaskFilters` above the table. Keep the delete/re-fetch behaviour working with filters.
- `frontend/src/api/types.ts` -- `TASK_STATUSES`, `TASK_PRIORITIES`, the derived types.

## Tasks & Acceptance

**Execution:**
- [x] `backend/TaskFlow.Api/Dtos/TaskListQuery.cs`, `Services/TaskService.cs` -- the filters and the single-pass validation.
- [x] `backend/TaskFlow.Api.Tests/Tasks/FilterTasksTests.cs` -- one test per API matrix row (the AC-3 values as a `[Theory]`).
- [x] `frontend/src/api/tasks.ts`, `components/TaskFilters.tsx`, `components/TaskTable.tsx` -- the API call, filter component and empty message.
- [x] `frontend/src/pages/TasksPage.tsx`, `pages/ProjectPage.tsx` -- URL-driven filters and the UI matrix rows.
- [x] `frontend/src/index.css` -- a small filter bar layout that wraps on narrow screens.

**Acceptance Criteria:**
- Given a filter is active, when I delete a task, then the list re-fetches with the same filters.
- Given the filter selects, when I tab through them, then each has a visible label and changes apply with the keyboard.

## Implementation Notes

- Review fixes: `TaskFilters.disabled` during deletes; filter-change effect resets to loading (one commented `oxlint-disable-next-line react/set-state-in-effect` per page, deliberate); `react/exhaustive-deps` enabled in `.oxlintrc.json` (no existing violations); tests for no-match 200 `[]`, unknown project + invalid filters (3 keys), priority `2` / ` High` / `Hgh`.
- Verification by the orchestrator (2026-10-26): `dotnet build` 0 warnings/0 errors; `dotnet test` 165/165; `npm run build` ok; `npm run lint` clean. First browser attempt invalid (ports 5173/5080 still held by leftover servers); rerun on free ports with a scratch DB: Todo+High → URL `/tasks?status=Todo&priority=High`, API `?status=Todo&priority=High`, 2 matching rows; reload keeps URL/selects/rows; Status → All removes `status`; Back restores it; `/projects/1?status=Done` → API `projectId=1&status=Done`, 1 row; `?status=bogus` → select All, API called without status, all rows. API `?status=7` → 400; `?projectId=9999&status=7&priority=7` → 3 keys. "No tasks match these filters." covered by the API no-match test and the implementer's browser run (the orchestrator's no-match step picked a matching combination).

## Spec Change Log

## Review Triage Log

| # | Layer | Finding | Verdict | Evidence / route |
|---|---|---|---|---|
| B1 / E2 | blind, edge | Filter change during a delete: the old-filter re-fetch can overwrite the newer list | low | `loadTasks` has no stale guard and selects stay enabled during delete. Smallest fix: disable filters while deleting → **patch**. |
| B2 / E3 | blind, edge | Old rows shown under new filters; empty message can describe the wrong list | low | Effect never resets to loading. Direct fix → **patch**. |
| B3 | blind | No test for a valid filter matching nothing → 200 `[]` | low | "UI no match" relies on it. Direct test → **patch**. |
| VG1 / B5 | verify, blind | Unknown project + invalid filters not tested together | medium | Pre-verified: a regression to the early return would pass. **patch**. |
| B4 | blind | Invalid-value theory lopsided; `status=Hgh` mislabelled | low | Direct test additions → **patch**. |
| VG2 | verify | Hook dependency arrays unchecked (stale closure regressions) | medium | Pre-verified; `react/exhaustive-deps` exists in the installed oxlint → **patch** (no new dependency). |
| B11 | blind | No automated frontend tests for URL/filter behaviour | medium | No FE test runner by architecture decision → **defer** (same decision as earlier stories). |
| B6 | blind | OpenAPI shows status/priority query params as free strings | low | Same rejection as 3.1 B6a / 3.2 B6b. Rejected. |
| B7 | blind | Junk values stay in the URL | low | Treated as All, never sent; cosmetic. Rejected. |
| B8 | blind | No "Clear filters" button | low | UX polish (Story 5.2). Rejected. |
| B9 | blind | Project count shows total above a filtered list | low | The count describes the project, not the view. Rejected. |
| B10 | blind | No aria-live announcement of results | low | Accessibility polish (Story 5.2). Rejected. |
| B12 | blind | Hard-coded select ids | low | One filter bar per page. Rejected. |
| E1 | edge | Repeated `?status=` keys bind the first value | low | Same rejection as 3.2 B4. Rejected. |

## Verification

**Commands:**
- `cd backend && dotnet build` -- expected: 0 errors, 0 warnings
- `cd backend && dotnet test` -- expected: all pass (146 existing + new)
- `cd frontend && npm run build && npm run lint` -- expected: success, no warnings

**Manual checks:**
- Browser on a scratch DB (headless Chrome via the DevTools protocol is fine): the UI rows of the matrix.
