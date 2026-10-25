---
title: 'Story 3.3 — Edit a task (including persistence after restart)'
type: 'feature'
created: '2026-10-26'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '6d9a513bd172c5b9fb0b9ad4ac8b675ca8690514'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/CLAUDE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A task can be created but never changed: not its status, its project or its due date. Nothing proves that an edit survives a restart (AC-4).

**Approach:** Add `PUT /api/tasks/{id}` as a full replace, by filling in the edit branch of `ValidateAndApplyAsync` (required fields, unchanged-past-due-date rule). Add an edit mode to the task form at `/tasks/:id/edit`, opened from the task title links. Add the AC-4 file-database restart test.

## Boundaries & Constraints

**Always:**
- One `ValidateAndApplyAsync`. `isNew: false` replaces the current `NotSupportedException`. Edit: title, projectId, status and priority are required (no defaults). Description: null/empty/whitespace clears it. Due date: equal to the stored value → accepted even if past; null → cleared; otherwise it must be ≥ today (local, from `TimeProvider`).
- PUT looks up the route id first (unknown → 404, before service validation), saves the tracked entity, and re-reads through `ToDto`.
- The task form edit mode mirrors `ProjectForm`: optional initial values; it clears on success only in create mode. Errors are shown by their inputs, typed values are kept, `''` → `null`, `projectId` is sent as a number, the date has no `min`.
- AC-4 test: `TaskFlowApiFactory` gets an optional connection-string constructor parameter. With a file DB it opens no keep-alive connection. The test calls `SqliteConnection.ClearAllPools()` before deleting the temp file.

**Never:**
- No task delete (Story 3.4), no filters (Story 4.1), no inline status edits in tables, no new dependencies.
- Never use the real `taskflow.db` in tests.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Update OK | PUT every field (status `"done"`, priority `"low"`) | 200 `TaskDto` re-read: trimmed title, `Done`, `Low`, new values | N/A |
| Move project | PUT `projectId` = Q (task was in P) | `projectName` = Q's name; `taskCount` P −1, Q +1 | N/A |
| Required on edit | PUT with null status / priority / title / projectId | 400 "Status is required." / "Priority is required." / "Title is required." / "Project is required." | nothing saved |
| Clears | description omitted / null / `""` / `"   "`; dueDate omitted / null | stored as null | N/A |
| Raw empty date | `"dueDate": ""` | 400 under `$.dueDate` | framework 400 |
| Same rules as create | whitespace title (AC-1); projectId 9999 (AC-2); status `"7"` (AC-3) | the create messages | nothing saved |
| Unchanged past date | created with dueDate = today, Clock +2 days, PUT the same date | 200 | N/A |
| New past date | same setup, PUT yesterday's date | 400 "Due date cannot be in the past." | nothing saved |
| Clear / future date | PUT dueDate null / Clock.Today + 5 | 200, cleared / 200, set | N/A |
| Unknown id | `PUT /api/tasks/9999` with a valid body, and with `{title:""}` | 404 both | N/A |
| AC-4 restart | file DB: factory A creates project + task, PUTs new title/status, is disposed; `ClearAllPools`; factory B on the same file | GET returns the updated values; temp file deleted | N/A |
| Open edit | click or Enter on a title in either task list | `/tasks/:id/edit`, every field pre-filled | load failure → banner |
| Edit errors | clear the title, Save | "Title is required." by the input; values kept | unmatched keys → banner |
| Edit save | change the project to Q, Save | navigate to `/projects/Q`; the task is listed there | N/A |
| Unknown edit page | `/tasks/9999/edit`, `/tasks/abc/edit` | `NotFoundPage` "Task not found." | N/A |

</frozen-after-approval>

## Code Map

- `backend/TaskFlow.Api/Services/TaskService.cs:110-117` -- private `ValidateAndApplyAsync` throws for `!isNew`; replace with the edit rules. `CreateAsync` (`:79`) shows the save + re-read pattern; add `UpdateAsync` after it.
- `backend/TaskFlow.Api/Controllers/TasksController.cs` -- `List`, `GetById`, `Create`; add `Update` (`[HttpPut("{id:int}")]`, 200/400/404 attributes), in the same `switch` style as `ProjectsController.Update`.
- `backend/TaskFlow.Api.Tests/TestInfrastructure/TaskFlowApiFactory.cs:19-30` -- the in-memory connection string and keep-alive connection; make both conditional on there being no connection string passed in.
- `frontend/src/components/TaskForm.tsx:13-45` -- props and `useState` seeds; add `initialValues` (follow `ProjectForm.tsx`'s pattern and its create-only clearing).
- `frontend/src/pages/TaskFormPage.tsx` -- create page; make it handle `/tasks/:id/edit` too (load the task + projects; 404 → `<NotFoundPage message="Task not found." />`), or add a small `TaskEditPage` that reuses `TaskForm`. Either is fine; keep one form component.
- `frontend/src/components/TaskTable.tsx` -- the title `<td>` becomes `<Link to={`/tasks/${task.id}/edit`}>`.
- `frontend/src/api/tasks.ts`, `frontend/src/App.tsx` -- add `getTask`, `updateTask`, and the `/tasks/:id/edit` route.

## Tasks & Acceptance

**Execution:**
- [x] `backend/TaskFlow.Api/Services/TaskService.cs` -- edit branch of `ValidateAndApplyAsync`; `UpdateAsync(id, request)`.
- [x] `backend/TaskFlow.Api/Controllers/TasksController.cs` -- `Update` action.
- [x] `backend/TaskFlow.Api.Tests/Tasks/UpdateTaskTests.cs` -- one test per API matrix row (Update OK through Unknown id).
- [x] `backend/TaskFlow.Api.Tests/TestInfrastructure/TaskFlowApiFactory.cs`, `backend/TaskFlow.Api.Tests/Persistence/TaskPersistenceTests.cs` -- the AC-4 row.
- [x] `backend/TaskFlow.Api.Tests/OpenApiTests.cs` -- PUT `/api/tasks/{id}` documents `200`, `400`, `404`.
- [x] `frontend/src/api/tasks.ts`, `components/TaskForm.tsx`, `components/TaskTable.tsx`, the edit page, `App.tsx` -- the UI matrix rows.

**Acceptance Criteria:**
- Given the running app, when I edit a task, stop and restart the API and reload, then the edit is still there (manual AC-4, noted in Implementation Notes).
- Given `TasksController.Update`, when I read it, then it holds no rules and no DbContext.

## Implementation Notes

- **Edit page:** a separate `pages/TaskEditPage.tsx` (route `/tasks/:id/edit`) reuses `TaskForm`; `TaskFormPage` stays create-only. It loads the task and the projects together (`Promise.all`); a 404 renders `NotFoundPage` "Task not found.", any other failure an `ErrorBanner`. A non-digit id is "not found" without calling the API, as on `ProjectPage`.
- **TaskForm:** new optional `initialValues: TaskFormValues` (the input strings; type lives in `taskFormFields.ts` so `TaskForm.tsx` exports only a component). `initialProjectId` stays for create mode. After a successful save the form resets to its create defaults only when `initialValues` is absent.
- **Due-date rule:** `keepsStoredDate = !isNew && request.DueDate == task.DueDate`, compared against the tracked entity before any value is copied onto it.
- **Status/priority on edit:** null/omitted → "… is required."; present but invalid → the create "must be one of" message.
- **AC-4 test:** `Persistence/TaskPersistenceTests.cs` uses a temp file (`Path.GetTempPath()`, never `taskflow.db`) with default pooling, calls `SqliteConnection.ClearAllPools()` between factories and before deleting the file (plus any `-wal`/`-shm` side files), and asserts the file is gone.
- **Manual AC-4 (2026-10-26):** headless Chrome via CDP against `dotnet run` on a scratch DB and `vite`: edited a task (title, project P→Q, status, description, cleared due date), stopped the API (edit page showed "Cannot reach the server. Is the API running?"), restarted the API on the same DB file, reloaded `/tasks/:id/edit` and `/tasks`: the edited values were still there.
- **Manual UI rows checked:** title links (click in `/tasks`, Enter in a project list) open a fully pre-filled edit page; the date input has no `min`; clearing the title shows "Title is required." by the input with typed values kept and nothing saved; moving to Q navigates to `/projects/Q` with the task listed and P empty; `''` description/date are sent as null; `/tasks/9999/edit` and `/tasks/abc/edit` show "Task not found."; no horizontal scroll at 390px. The unchanged-past-date row was not checked in the browser (the real clock cannot be moved); it is covered by `UpdateTaskTests`.

- Review fixes: `Update` summary narrowed (framework 400s unreadable bodies first); TaskForm's dead clear-after-create branch removed; description-too-long and empty status/priority edit tests; `Assert.Single` added; persistence-test cleanup made best-effort.
- Verification by the orchestrator (2026-10-26): `dotnet build` 0 warnings/0 errors; `dotnet test` 140/140; `npm run build` ok; `npm run lint` clean. Headless Chrome on a scratch file DB: Enter on a title in `/tasks` → `/tasks/1/edit`, every field pre-filled; empty title → "Title is required.", other values kept; save (title "Write README v2", Done, moved to Course notes, description emptied) → `/projects/2` lists it, old project shows 0 tasks; `/tasks/9999/edit` and `/tasks/abc/edit` → "Task not found.". **Manual AC-4:** API stopped and restarted on the same DB file → GET `/api/tasks/1` and the browser (`/tasks`, edit page) still show the edited values with description null.

## Spec Change Log

## Review Triage Log

| # | Layer | Finding | Verdict | Evidence / route |
|---|---|---|---|---|
| B1 / E5 | blind, edge | `Update` summary says 404 even for invalid bodies; unreadable bodies are framework 400s | low | Same wording issue as Story 2.2 B1. **patch** (comment). |
| B2 | blind | TaskForm's clear-after-create branch never has a visible effect (both pages navigate away) | low | Dead code in a beginner codebase (NFR-9); direct deletion → **patch**. |
| B5 | blind | Description length untested on update; one test lacks `Assert.Single` | low | Direct test additions → **patch**. |
| B6 | blind | Empty status/priority on edit: required vs must-be-one-of message unpinned | low | Direct test → **patch**. |
| B7 / E7 | blind, edge | Cleanup `File.Delete` in `finally` can mask the real failure | low | Direct best-effort try/catch → **patch**. |
| VG1 | verify | Edit-page/TaskForm behaviour has no automated frontend checks | medium | No FE test runner by architecture decision → **defer** (same decision as Stories 1.1–3.2). |
| B3 | blind | 404 on save (task deleted elsewhere) shows a banner, not "Task not found." | low | Needs a second tab; same rejection as 2.2 B4. Rejected. |
| B4 / E1 / E2 / E3 | blind, edge | Races between FindAsync/AnyAsync and save → 500 | low | Concurrent requests; single user (as earlier stories). Rejected. |
| E4 | edge | Two concurrent PUTs → lost update | false | PRD §6 out of scope: "Handling simultaneous edits from several browser tabs: the last save wins." |
| E6 | edge | Passing an in-memory string to the factory skips keep-alive | low | No caller does; the parameter is documented for file DBs. Rejected. |
| B8 | blind | OpenAPI test checks codes, not body schemas | low | Codes are the documented contract we pin; schema refs follow from `ProducesResponseType<T>`. Rejected. |
| B9 | blind | `initialProjectId` and `initialValues` can both be passed | low | Discriminated union adds type complexity for one caller each. Rejected. |
| B10 | blind | No Cancel; save always goes to the project page | low | Spec/epic: saving returns to the task's project page; a back link exists. Rejected. |

## Verification

**Commands:**
- `cd backend && dotnet build` -- expected: 0 errors, 0 warnings
- `cd backend && dotnet test` -- expected: all pass (107 existing + new)
- `cd frontend && npm run build && npm run lint` -- expected: success, no warnings

**Manual checks:**
- Browser (headless Chrome via the DevTools protocol is fine) on a scratch DB: the UI matrix rows, plus edit → restart the API → reload → the edit is still there.
