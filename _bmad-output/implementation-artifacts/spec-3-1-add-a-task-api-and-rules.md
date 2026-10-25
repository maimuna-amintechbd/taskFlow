---
title: 'Story 3.1 — Add a task to a project (API and rules)'
type: 'feature'
created: '2026-10-25'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '6da473012cf46278f934537590b7cf91e128d91d'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-3-context.md'
  - '{project-root}/CLAUDE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Projects exist but cannot hold tasks, and nothing yet enforces the task rules (title, existing project, status/priority names, due date).

**Approach:** Add `TaskItem` with a database-enforced cascade from `Project`, `POST /api/tasks` and `GET /api/tasks/{id}`, a single-pass `TaskService` validator, the shared `EnumNames` parser and an injectable clock. Real task counts flow into projects. API only; the UI comes in Story 3.2.

## Boundaries & Constraints

**Always:**
- Follow epic-3-context exactly: entity shapes, cascade config, the `CreatedAt` UTC converter, `TimeProvider`, `EnumNames`, messages, keys, `ToDto`/re-read and test setup.
- `ValidateAndApplyAsync(request, task, isNew)` has the final signature, but only create (`isNew: true`) is wired and tested now. Update-only rules belong to Story 3.3.
- Status and priority in `SaveTaskRequest` are `string?`. On create, null → `Todo`/`Medium`; an empty or invalid value is an error.
- Today is `DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime)`; nothing calls `DateTime.Now`/`UtcNow`/`Today`.

**Never:**
- No `GET /api/tasks` list, `PUT`, `DELETE`, `TaskListQuery` or frontend changes (Stories 3.2–3.4).
- No `Enum.TryParse`/`Enum.IsDefined` on input, no DataAnnotations validation, no new packages, no `HasMaxLength`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Create defaults | `{title:"  Write README  ", projectId:P}` | 201, `Location /api/tasks/{id}`; title "Write README", status `Todo`, priority `Medium`, dueDate null, `projectName`, `createdAt` ending in `Z` | N/A |
| Get by id | existing / 9999 | 200 same `TaskDto` / 404 ProblemDetails | N/A |
| Task count | after create | `GET /api/projects` shows `taskCount: 1` for P | N/A |
| Title (AC-1) | missing, `""`, `"   "` / 201 chars after trim / 200 chars plus spaces | 400 "Title is required." / 400 "Title must be at most 200 characters." / 201 | nothing saved |
| Project (AC-2) | missing / 9999 | 400 `projectId` "Project is required." / "Project 9999 does not exist." | nothing saved |
| Names accepted | status `"inprogress"`, `"DONE"`; priority `"high"`, `"HIGH"`, `"High"` | 201; returned as `"InProgress"`/`"Done"`/`"High"` | N/A |
| Names rejected (AC-3) | `"7"`, `"2"`, `""`, `" Todo"`, `"Todo,Done"`, `"Hgh"` for status, and the same for priority | 400 "Status must be one of: Todo, InProgress, Done." / "Priority must be one of: Low, Medium, High." | nothing saved |
| JSON number | `"status": 2` | 400 with key `$.status` | framework 400 |
| Due date | `Clock.Today - 1` / today / tomorrow / null | 400 "Due date cannot be in the past." / 201 / 201 / 201 | nothing saved |
| Single pass | empty title, project 9999, status `"7"`, 2001-char description | one 400 with `title`, `projectId`, `status`, `description` | nothing saved |
| Cascade | delete a project that has tasks (API) | 204; its `TaskItems` rows are gone (checked via `factory.Services` DbContext) | N/A |
| FK enforced | insert a `TaskItem` with ProjectId 9999 directly via the DbContext | `SaveChangesAsync` throws `DbUpdateException` | N/A |

</frozen-after-approval>

## Code Map

- `backend/TaskFlow.Api/Models/Project.cs` -- add `List<TaskItem> Tasks { get; set; } = [];`.
- `backend/TaskFlow.Api/Data/TaskFlowDbContext.cs` -- only `Projects` today; add `TaskItems` and `OnModelCreating` (cascade, enum text conversions, `CreatedAt` converter).
- `backend/TaskFlow.Api/Services/ProjectService.cs:23-24` -- `ToDto` hard-codes `0`; change to `p.Tasks.Count()`. `DeleteAsync` stays as is (it doesn't load tasks).
- `backend/TaskFlow.Api/Program.cs:28-32` -- next to `AddDbContext`/`AddScoped<ProjectService>`: add `AddSingleton(TimeProvider.System)`, `AddScoped<TaskService>()`.
- `backend/TaskFlow.Api.Tests/TestInfrastructure/` -- `TaskFlowApiFactory` (add `Clock`, replace `TimeProvider` in `ConfigureTestServices`), `HttpClientExtensions` (add `CreateTaskAsync`), `ApiJson`.
- Reuse `ServiceResult`, and the controller `switch` style from `ProjectsController`.

## Tasks & Acceptance

**Execution:**
- [x] `backend/TaskFlow.Api/Models/TaskItem.cs`, `TaskItemStatus.cs`, `TaskPriority.cs`, `Project.cs` -- the entities per AD-5.
- [x] `backend/TaskFlow.Api/Data/TaskFlowDbContext.cs` -- `DbSet<TaskItem> TaskItems`; `OnModelCreating` as in the Code Map.
- [x] `backend/TaskFlow.Api/Data/Migrations/` -- `dotnet ef migrations add AddTaskItems --project backend/TaskFlow.Api --output-dir Data/Migrations`; don't edit it by hand. Confirm the migration has `onDelete: ReferentialAction.Cascade`.
- [x] `backend/TaskFlow.Api/Services/EnumNames.cs` -- `TryParse<TEnum>`.
- [x] `backend/TaskFlow.Api/Dtos/SaveTaskRequest.cs` (class, nullable properties), `Dtos/TaskDto.cs` (record).
- [x] `backend/TaskFlow.Api/Services/TaskService.cs` -- `GetByIdAsync`, `CreateAsync`, `ValidateAndApplyAsync`, `ToDto` (includes `p.Project.Name`).
- [x] `backend/TaskFlow.Api/Controllers/TasksController.cs` -- `GetById`, `Create`, with `ProducesResponseType` attributes (200/404; 201/400 `ValidationProblemDetails`).
- [x] `backend/TaskFlow.Api/Services/ProjectService.cs`, `Program.cs` -- as in the Code Map.
- [x] `backend/TaskFlow.Api.Tests/TestInfrastructure/FixedTimeProvider.cs`, factory, helpers -- the clock is settable, `LocalTimeZone` is UTC, and it exposes `Today`.
- [x] `backend/TaskFlow.Api.Tests/Tasks/CreateTaskTests.cs`, `GetTaskTests.cs`, `EnumNamesTests.cs`, `Projects/CascadeAndForeignKeyTests.cs` -- one test per matrix row; enum cases as `[Theory]`.
- [x] `backend/TaskFlow.Api.Tests/OpenApiTests.cs` -- deferred-work item: assert the `TaskDto` `status` schema in `/openapi/v1.json` lists the names `Todo`/`InProgress`/`Done` (not integers). If it doesn't, also register the converter via `ConfigureHttpJsonOptions` in `Program.cs`.

**Acceptance Criteria:**
- Given an existing `taskflow.db` from Epic 2, when the API starts, then `AddTaskItems` applies and the existing projects remain.
- Given the API is running, when I create a task through Swagger UI, then the project's task count increases on the Projects page and the project page (manual).

## Implementation Notes

- OpenAPI deferred item resolved: `/openapi/v1.json` listed `TaskItemStatus`/`TaskPriority` as integers; fixed by also registering the converter via `ConfigureHttpJsonOptions`; test covers status and priority.
- `ValidateAndApplyAsync` has the final signature and is private; `isNew: false` throws `NotSupportedException` until Story 3.3 adds the edit rules.
- Review fixes: settable `FixedTimeProvider.LocalZone` + near-midnight UTC+5 test (proven to fail when TaskService uses the UTC date); description normalisation tests; factory guard for Program.cs's TimeProvider; FK test asserts SQLite error 19; entity defaults Todo/Medium; rejection values tightened (spec's " Todo", "Todo,Done", "Hgh" kept for status); named args.
- Verification (2026-10-25): `dotnet build` 0 warnings/0 errors; `dotnet test` 96/96; `npm run build` ok; `dotnet ef migrations has-pending-model-changes` → none. Upgrade: DB migrated to `AddProjects` with one project → API start applied `AddTaskItems`, project kept, SQLite FK `ON DELETE CASCADE`; POST "  Write README  "/priority "high" → 201 Todo/High, createdAt …Z; project `taskCount` 0 → 1; `/projects` and `/projects/1` show "1 task" (headless Chrome); Swagger `TaskItemStatus` enum = Todo/InProgress/Done.

## Spec Change Log

## Review Triage Log

| # | Layer | Finding | Verdict | Evidence / route |
|---|---|---|---|---|
| VG1 / B1 | verify, blind | No test distinguishes local "today" from UTC | medium | Pre-verified: clock fixed at UTC 09:30. **patch**: settable zone + near-midnight test. |
| VG3 / B5 | verify, blind | Description trim / whitespace→null / 2000 boundary untested | medium | Pre-verified. **patch**. |
| VG2 | verify | Production `TimeProvider` registration never exercised | medium | Pre-verified: factory always replaces it. **patch**: guard in factory. |
| B3 | blind | FK test accepts any `DbUpdateException`; comment wrong | medium | Test could pass on a NOT NULL failure. **patch**: assert SQLite code 19. |
| B4 | blind | Entity default Priority `Low` vs API `Medium` | low | Any TaskItem built outside the service gets Low (e.g. FK test, Story 3.3+). Direct fix → **patch**. |
| B8 | blind | Some enum rejection cases fail for the wrong reason | low | `" Todo"` isn't a priority name. Direct test-data fix → **patch**. |
| B6b | blind | OpenAPI enum test checks only status | low | Direct assertion → **patch**. |
| B12b | blind | `ValidateAndApplyAsync` public while throwing for edit | low | Direct visibility fix → **patch**. |
| B12a | blind | Positional same-typed string args in a test helper call | low | Direct → **patch**. |
| B12 | blind | Snapshot/Designer not reviewable | false | `dotnet ef migrations has-pending-model-changes` → "No changes have been made to the model since the last migration"; snapshot has Cascade + IsRequired. |
| E1 / B2 | edge, blind | Project deleted between `AnyAsync` and save → 500 | low | Needs concurrent requests; single user (as 2.1 E3 / 2.2 B11). Rejected. |
| E2 | edge | Cascade during re-read → null DTO | low | Same race class. Rejected. |
| E3 | edge | Client time zone behind server | false | PRD §3/§11: "today" is the backend machine's local date; browser and backend on the same machine. |
| E4 | edge | Zero-width-only title accepted | low | Unlikely input (as 2.1 E5). Rejected. |
| B6a | blind | OpenAPI request schema shows status/priority as free strings | low | Request type is `string?` by AD-4; response schema lists names; UI uses its own const arrays. Rejected. |
| B7 | blind | No automated Epic-2→3 upgrade test | low | Verified manually today on a DB migrated to `AddProjects` (project kept, task created, count 1); Story 3.3 adds the file-DB restart test. Rejected. |
| B9 | blind | Untested binder 400s for priority/projectId/dueDate types | low | AD-3 documents framework 400 under `$.` keys; client normalises keys. Rejected. |
| B10 | blind | 400 body shape beyond `errors` unasserted | low | Shape fixed by AD-2; frontend reads only `title`/`errors`. Rejected. |
| B11 | blind | `CreatedAt` converter doesn't normalise on write | low | Only TaskService writes it, from `GetUtcNow().UtcDateTime`. Rejected. |

## Verification

**Commands:**
- `cd backend && dotnet build` -- expected: 0 errors, 0 warnings
- `cd backend && dotnet test` -- expected: all pass (29 existing + new)
- `cd frontend && npm run build` -- expected: success (no frontend changes, still green)

**Manual checks:**
- Start the API on a DB created before this story → it migrates; create a task via Swagger/curl → the project count is 1 on `/projects` and `/projects/:id`.
