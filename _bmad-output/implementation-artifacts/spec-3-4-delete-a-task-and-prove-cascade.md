---
title: 'Story 3.4 — Delete a task, and prove the project cascade'
type: 'feature'
created: '2026-10-26'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'ac24db76cf673a417527f971593d4ffce27c02af'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/CLAUDE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A single task cannot be deleted, and release check AC-5 (deleting a project deletes all its tasks) is not yet proven in the form the release checklist names.

**Approach:** Add `DELETE /api/tasks/{id}` and a Delete button on every task row (Tasks page and project task view) with confirmation. Add the AC-5 integration test exactly as the plan words it. Check in the browser that the project-delete prompt shows the real task count and that the tasks are gone afterwards.

## Boundaries & Constraints

**Always:**
- `TaskService.DeleteAsync(id)`: unknown → NotFound; otherwise remove and save → `ServiceResult.Ok()`. Controller `Delete` → 204/404 with `ProducesResponseType`, the same `switch` style.
- `ProjectService.DeleteAsync` stays as is (removes only the project row; the database cascade removes the tasks).
- `TaskTable` stays presentational: it gets an `onDelete(task)` callback prop and renders a native `button` (class `button-danger`) per row with an accessible name that includes the task title. Pages own `window.confirm(`Delete task "${title}"?`)`, the API call and the re-fetch. The project page re-fetches the project too, so its task count updates. A failure shows `ErrorBanner`.

**Never:**
- No soft delete, undo, bulk delete or custom modal; no new dependencies; no change to the cascade configuration or migrations.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Delete task | `DELETE /api/tasks/{id}` | 204; then GET → 404; the project's `taskCount` −1 | N/A |
| Delete unknown | `DELETE /api/tasks/9999` | 404 ProblemDetails | N/A |
| AC-5 cascade | project A with 3 tasks, project B with 1; `DELETE /api/projects/A` | 204; a DbContext from `factory.Services` counts 0 `TaskItems` with A's id; GET each of A's 3 tasks → 404; B's task unchanged (GET 200, same values) | N/A |
| FK enforced | insert a `TaskItem` with ProjectId 9999 via the DbContext | `DbUpdateException` with inner SQLite error 19 (existing test; keep it green) | N/A |
| UI delete task | Delete on a row → confirm `Delete task "X"?` → OK | the row disappears after a re-fetch; on the project page the count drops | failure → `ErrorBanner` |
| UI cancel | same, Cancel | nothing changes, no request sent | N/A |
| UI project cascade | a project with 2 tasks → Delete project | prompt `Delete project "X" and its 2 tasks?`; after OK `/tasks` no longer lists them | N/A |

</frozen-after-approval>

## Code Map

- `backend/TaskFlow.Api/Services/TaskService.cs` -- `UpdateAsync` (`:103`) shows the `FindAsync` + NotFound pattern; add `DeleteAsync` after it (mirror `ProjectService.DeleteAsync`, `ProjectService.cs:88-98`).
- `backend/TaskFlow.Api/Controllers/TasksController.cs` -- add `[HttpDelete("{id:int}")] Delete` (mirror `ProjectsController.Delete`).
- `backend/TaskFlow.Api.Tests/Projects/CascadeAndForeignKeyTests.cs` -- has `DeleteProject_WithTasks_DatabaseRemovesItsTasks` (`:29`) and the FK test (`:47`); add or upgrade a test to the full AC-5 row (3 + 1 tasks, GET 404 for each, the other project untouched). Don't weaken the existing assertions.
- `backend/TaskFlow.Api.Tests/OpenApiTests.cs` -- add DELETE `/api/tasks/{id}` → `204`, `404`.
- `frontend/src/components/TaskTable.tsx` -- header row `:20-27`, row `:31-41`; add an "Actions" column and the button.
- `frontend/src/pages/TasksPage.tsx` (`useEffect` + `listTasks` at `:18-21`) and `frontend/src/pages/ProjectPage.tsx` (`loadTasks` `:65`, `<TaskTable>` `:233`, `loadProject`) -- add the delete handlers; reuse `messageOf` from `client.ts`.
- `frontend/src/api/tasks.ts` -- add `deleteTask(id)` (`request<void>`, like `deleteProject`).

## Tasks & Acceptance

**Execution:**
- [x] `backend/TaskFlow.Api/Services/TaskService.cs`, `Controllers/TasksController.cs` -- the delete endpoint.
- [x] `backend/TaskFlow.Api.Tests/Tasks/DeleteTaskTests.cs` -- the Delete task / Delete unknown rows.
- [x] `backend/TaskFlow.Api.Tests/Projects/CascadeAndForeignKeyTests.cs` -- the AC-5 row.
- [x] `backend/TaskFlow.Api.Tests/OpenApiTests.cs` -- the DELETE responses.
- [x] `frontend/src/api/tasks.ts`, `components/TaskTable.tsx`, `pages/TasksPage.tsx`, `pages/ProjectPage.tsx` -- the UI rows.

**Acceptance Criteria:**
- Given `TasksController.Delete`, when I read it, then it holds no rules and no DbContext.
- Given the Delete button, when I Tab to it, then it is reachable and its accessible name names the task.

## Implementation Notes

- Review fixes: page-level `deletingTask` guard + `TaskTable.deleteDisabled` (also during project delete; project Delete disabled during a task delete, added by the orchestrator); re-fetch after every delete attempt; `taskDeleteError` cleared on edit/project delete; TasksPage comment corrected; AC-5 test also asserts project A → 404, comments on both cascade tests; delete-twice and last-task tests.
- Verification by the orchestrator (2026-10-26): `dotnet build` 0 warnings/0 errors; `dotnet test` 146/146; `npm run build` ok; `npm run lint` clean. Headless Chrome on a scratch DB with DELETE requests counted: Cancel → prompt `Delete task "Beta two"?`, 0 DELETE requests, rows unchanged; OK → 1 DELETE, row gone; project page "2 tasks" → "1 task" after a task delete; project delete prompt `Delete project "Alpha" and its 2 tasks?` → `/projects`, `/tasks` lists only Beta's task, GET `/api/tasks/1` and `/2` → 404.

## Spec Change Log

## Review Triage Log

| # | Layer | Finding | Verdict | Evidence / route |
|---|---|---|---|---|
| B1 / E2 / E3 / E9 | blind, edge | No in-flight guard on task delete (project delete has one); task delete possible during project delete | low | Inconsistent with the existing project-delete pattern; small state + `disabled` → **patch**. |
| B2 / E4 / E5 / VG-o | blind, edge, verify | Failed delete never re-fetches; ghost row keeps failing | low | Direct: re-fetch after every attempt → **patch**. |
| B9 / E6 / E7 | blind, edge | `taskDeleteError` lingers next to fresh data | low | Direct clears → **patch**. |
| B8 | blind | TasksPage `loadTasks` comment contradicts behaviour | low | Comment fix → **patch**. |
| B7 | blind | AC-5 test doesn't assert project A is gone; overlaps older test | low | Direct assertion + comments → **patch**. |
| B5 | blind | Missing delete-twice and last-task tests | low | Cheap, direct → **patch**. |
| VG1 | verify | Task-delete UI has no automated check | medium | No FE test runner by architecture decision → **defer** (same decision as Stories 1.1–3.3). |
| B4 / E1 | blind, edge | Row removed between FindAsync and save → 500 | low | Concurrent requests; single user (same as earlier stories; ProjectService mirrors it). Rejected. |
| B3 | blind | Focus lost / no success announcement after delete | low | Accessibility polish belongs to Story 5.2. Rejected. |
| B6 | blind | OpenAPI test checks codes, not 204-no-content / ProblemDetails schema | low | Same rejection as 3.3 B8. Rejected. |
| B10 | blind | No verification evidence recorded | false | Orchestrator ran build/test/lint (144/144) before review; results recorded in Implementation Notes at step 5. |
| E8 | edge | setState after unmount on navigation mid-delete | low | React ignores it; same as earlier rejections. Rejected. |
| B8b | blind | TasksPage keeps two copies of its loader | low | Same pattern as the other pages (effect with ignore guard + callback). Rejected. |

## Verification

**Commands:**
- `cd backend && dotnet build` -- expected: 0 errors, 0 warnings
- `cd backend && dotnet test` -- expected: all pass (140 existing + new)
- `cd frontend && npm run build && npm run lint` -- expected: success, no warnings

**Manual checks:**
- Browser on a scratch DB (headless Chrome via the DevTools protocol is fine): the three UI rows.
