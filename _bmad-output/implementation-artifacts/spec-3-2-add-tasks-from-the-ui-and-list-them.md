---
title: 'Story 3.2 — Add tasks from the UI and see them newest first'
type: 'feature'
created: '2026-10-25'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '3df21f1a82da850c028d02a222954eac06e74b02'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/CLAUDE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Tasks can only be created through the API, and there is no way to see a list of them.

**Approach:** Add `GET /api/tasks` (newest first, optional strictly validated `projectId` filter), a task form at `/tasks/new`, a shared `TaskTable`, the Tasks page, and the project task view with an "Add task" link on the project page.

## Boundaries & Constraints

**Always:**
- Follow epic-3-context: `TaskListQuery` (class, `{get;set;}`, explicit `[FromQuery]`, `[DisplayFormat(ConvertEmptyStringToNull = false)]` on `ProjectId`), `int.TryParse` in the service, exact messages, `ToDto` reuse.
- Frontend follows the project patterns: only pages import `src/api`; components take props; `noValidate`, no HTML constraint attributes (the date input has no `min`); `''` → `null`; `projectId` sent as a number; dates stay `yyyy-MM-dd` strings; re-fetch after changes; an unmatched error key or the error message goes to `ErrorBanner`.
- `TASK_STATUSES`/`TASK_PRIORITIES` are `as const` arrays in `types.ts`; status/priority types are derived from them; selects render from them.

**Never:**
- No status/priority filters (Story 4.1), edit page, title links, or per-row Delete (Stories 3.3–3.4).
- No new dependencies, no global state, no JS `Date` for due dates, no inline status edits.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| List all | 3 tasks created at increasing `Clock` times | 200, newest first | N/A |
| Same instant | 2 tasks with equal `createdAt` | ordered by id descending | N/A |
| Project filter | `?projectId=P` (two projects with tasks) | only P's tasks, newest first | N/A |
| Unknown project | `?projectId=9999` | 400 `projectId` "Project 9999 does not exist." | N/A |
| Not a number | `?projectId=abc` / `?projectId=` | 400 `projectId` "Project id must be a whole number." | N/A |
| No tasks | `GET /api/tasks` on an empty DB | 200 `[]` | N/A |
| Tasks page | `/tasks` | `TaskTable`: title, project, status, priority, due date (blank if null), newest first; empty → "No tasks yet." | load failure → `ErrorBanner` |
| Project task view | `/projects/:id` | below the details: the same table with only this project's tasks; empty → "No tasks yet."; an "Add task" link | load failure → `ErrorBanner` |
| Form defaults | "Add task" → `/tasks/new?projectId=P` | labelled Title, Description, Project (P preselected), Status `Todo`, Priority `Medium`, Due date (no `min`) | N/A |
| Form errors | empty title + yesterday's date | "Title is required." and "Due date cannot be in the past." by their inputs; values kept | unmatched keys → banner |
| Form success | valid input | navigate to `/projects/P`; the new task is first in the list; the count went up | N/A |
| No project chosen | `/tasks/new` (no query), submit | "Project is required." by the Project select | N/A |

</frozen-after-approval>

## Code Map

- `backend/TaskFlow.Api/Services/TaskService.cs` -- has `ToDto`, `FindDtoAsync`, `GetByIdAsync`, `CreateAsync`, private `ValidateAndApplyAsync`; add `ListAsync(TaskListQuery)` returning `ServiceResult<List<TaskDto>>` (Invalid for a bad `projectId`).
- `backend/TaskFlow.Api/Controllers/TasksController.cs` -- `GetById`, `Create`; add `List([FromQuery] TaskListQuery query)` → 200/400 with `ProducesResponseType`.
- `backend/TaskFlow.Api.Tests/TestInfrastructure/` -- `factory.Clock` (move `UtcNow` between creates), `CreateTaskAsync`, `CreateProjectAsync`.
- `frontend/src/api/types.ts` -- only project types today. `frontend/src/api/projects.ts` -- the pattern for `tasks.ts`.
- `frontend/src/components/ProjectForm.tsx`, `projectFormFields.ts` -- the pattern for `TaskForm` + `taskFormFields.ts` (create mode now; Story 3.3 adds `initialValues`).
- `frontend/src/pages/ProjectPage.tsx` -- `ProjectDetails` view; add the task section and the "Add task" link below the details (keep edit/delete as is).
- `frontend/src/pages/TasksPage.tsx` (placeholder), `App.tsx` (add `/tasks/new`), `components/EmptyState.tsx`, `ErrorBanner.tsx`.

## Tasks & Acceptance

**Execution:**
- [x] `backend/TaskFlow.Api/Dtos/TaskListQuery.cs` -- class with `ProjectId` only (`string?` + `DisplayFormat`); Story 4.1 adds status and priority.
- [x] `backend/TaskFlow.Api/Services/TaskService.cs` -- `ListAsync`: parse/validate `ProjectId`, filter, `OrderByDescending(CreatedAt).ThenByDescending(Id)`, `AsNoTracking().Select(ToDto)`.
- [x] `backend/TaskFlow.Api/Controllers/TasksController.cs` -- `List` action.
- [x] `backend/TaskFlow.Api.Tests/Tasks/ListTasksTests.cs` -- one test per API matrix row.
- [x] `frontend/src/api/types.ts` -- `TASK_STATUSES`, `TASK_PRIORITIES`, `TaskItemStatus`, `TaskPriority`, `TaskDto`, `SaveTaskRequest` (`T | null`).
- [x] `frontend/src/api/tasks.ts` -- `listTasks(filters: { projectId?: number })`, `createTask(body)`.
- [x] `frontend/src/components/TaskTable.tsx` -- presentational table (`<th scope="col">`), plain-text titles.
- [x] `frontend/src/components/TaskForm.tsx`, `taskFormFields.ts` -- labelled inputs, projects passed in as a prop, `onSubmit`, `fieldErrors`, a split helper like `splitProjectFieldErrors`.
- [x] `frontend/src/pages/TaskFormPage.tsx` + route `/tasks/new` in `App.tsx` -- loads projects, reads `projectId` from `useSearchParams`, on success navigates to `/projects/{projectId}`.
- [x] `frontend/src/pages/TasksPage.tsx`, `pages/ProjectPage.tsx` -- lists as in the matrix.
- [x] `frontend/src/index.css` -- table and form styles; long titles wrap.

**Acceptance Criteria:**
- Given `TasksController.List`, when I read it, then it holds no rules and no DbContext.
- Given the browser, when I tab through the task form, then every input has a visible label and the submit works with Enter.

## Implementation Notes

- Review fixes: empty-project list test; `OpenApi_ListTasks_DocumentsResponses`; `key={queryProjectId}` on TaskForm; `FALLBACK_MESSAGE`/`messageOf` moved to `client.ts` (DashboardPage keeps its inline copy until Story 4.2 rewrites it); `TASK_FORM_FIELDS` typed with `satisfies`.
- Verification (2026-10-25): `dotnet build` 0 warnings/0 errors; `dotnet test` 107/107; `npm run build` ok; `npm run lint` clean. Headless Chrome on a scratch DB: `/tasks` empty → "No tasks yet."; project page shows Tasks section + "Add task"; form at `/tasks/new?projectId=1` preselects project 1, Todo/Medium, date input has no `min`; empty title + yesterday → both messages by their inputs, date kept; valid save → `/projects/1`, task listed, count 0 → 1; `/tasks` newest first ("Plan launch" above "Write README"), blank due date shown; rename project → task row shows the new name; `/tasks/new` without project → "Project is required."; `?projectId=1` → `?projectId=2` preselects 2.

## Spec Change Log

## Review Triage Log

| # | Layer | Finding | Verdict | Evidence / route |
|---|---|---|---|---|
| B5 | blind | No test: existing project with no tasks → 200 `[]` | medium | First state of the project task view; separates empty from 400. **patch**. |
| B6 / VG1 | blind, verify | `GET /api/tasks` OpenAPI responses unpinned | medium | Pre-verified gap. **patch**. |
| B9 | blind | `/tasks/new` keeps old preselection when only the query changes | low | `useState` initial value; one-attribute fix (`key`), same pattern as ProjectPage → **patch**. |
| B11 | blind | `FALLBACK_MESSAGE`/`messageOf` copied into four pages | low | Four copies will drift (NFR-9 readability); mechanical move to `client.ts` → **patch**. |
| B12 | blind | `TASK_FORM_FIELDS` not tied to `SaveTaskRequest` | low | `satisfies` is a direct, zero-runtime fix → **patch**. |
| VG2 | verify | No automated check for frontend logic (rename → task rows, preselection, error split) | medium | No FE test runner by architecture decision → **defer** (joins Stories 1.1/2.1/2.2 entry); rename check done manually this story. |
| B1 / E5 | blind, edge | List-load 400 banner shows the title, drops `errors.projectId` text | low | Only reachable when the project is deleted in another tab; still shows a banner, never blank. Rejected. |
| B2 | blind | `/projects/9999` also requests its tasks (wasted 400, never shown) | low | No user-visible effect; fix restructures loading. Rejected. |
| B3 / E1 | blind, edge | `?projectId=99999999999` says "must be a whole number"; `-1` → "does not exist" | low | Unrealistic ids; `-1` matches the body rule. Rejected. |
| B4 / E2 | blind, edge | Repeated `?projectId=` keeps the first value | low | The UI never sends it; adds binding complexity. Rejected. |
| B6b | blind | Swagger shows `projectId` query as string | low | By design (AD-4: string so bad values reach the service). Rejected. |
| B7 | blind | Status shown as "InProgress" | low | Display labels belong to Story 5.2 polish. Rejected. |
| B8 | blind | Back link on the form follows the query / changes after load | low | Cosmetic; link still useful. Rejected. |
| B10 | blind | Form still shown when there are no projects | low | A note links to project creation; submitting shows "Project is required.". Rejected. |
| E3 / E4 | edge | Races (project deleted between check and query; overlapping task fetches) | low | Concurrent requests; single user (same class as earlier stories). Rejected. |
| E6 | edge | Partially typed date → `''` → task saved without due date | low | Browser date pickers rarely leave partial input; fix needs validity handling. Rejected (candidate for 5.2). |

## Verification

**Commands:**
- `cd backend && dotnet build` -- expected: 0 errors, 0 warnings
- `cd backend && dotnet test` -- expected: all pass (96 existing + new)
- `cd frontend && npm run build && npm run lint` -- expected: success, no warnings

**Manual checks:**
- Browser (headless Chrome via the DevTools protocol is fine): the form, list and project-view rows of the matrix.
