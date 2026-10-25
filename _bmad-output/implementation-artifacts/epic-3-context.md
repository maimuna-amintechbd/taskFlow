# Epic 3 Context: Track tasks inside projects

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Let the user add tasks to a project, see them newest first in the all-tasks list and in each project's task view, edit them (including moving them to another project, with the due-date rules), and delete them. Deleting a project removes its tasks through the database cascade. This is the heart of the app: it introduces the `TaskItem` entity, the shared status/priority name parser, the injectable clock for "today", and the tests that prove a task needs a valid title and project, that bad status/priority values are rejected, that edits survive a restart, and that the cascade really happens.

## Stories

- Story 3.1: Add a task to a project (API and rules)
- Story 3.2: Add tasks from the UI and see them newest first
- Story 3.3: Edit a task (including persistence after restart)
- Story 3.4: Delete a task, and prove the project cascade

## Requirements & Constraints

- **Title:** required, 1–200 characters after trimming, saved trimmed. **Description:** optional, at most 2,000 characters; null or whitespace-only is stored as null.
- **Project:** required and must exist. An unknown `projectId` in a body or query string is a 400 under `projectId`, not a 404.
- **Status/priority:** create defaults to `Todo` / `Medium`. Only the exact names are accepted, ignoring case (`high` → `High`). Numbers (even valid ones like `2`), empty strings, surrounding whitespace, commas and misspellings are all rejected.
- **Due date:** optional date (no time). On create it must be today or later. On edit, an unchanged date is accepted even if it is now in the past, a new past date is rejected, and null clears it. "Today" is the backend machine's local date.
- **Edit is a full replace:** title, project, status and priority are required, with no defaults. An absent or empty description or due date is cleared.
- **Lists:** each task shows title, project, status, priority and due date; ordered by creation time descending, then id descending. Unknown task id → 404 on GET, PUT and DELETE.
- **Delete:** confirmation in the UI. A project delete removes all its tasks, enforced by the database. The project-delete prompt states how many tasks go with it.
- **Errors:** every write is validated on the backend, returning one 400 with every failing field at once; the UI shows each message next to its input and keeps what the user typed.
- **Release checks proven here:** valid title (AC-1), valid project (AC-2), invalid status/priority in the body (AC-3), task edits survive a restart (AC-4: one temp-file test plus a manual check noted in completion notes), cascade delete (AC-5).
- **Done means:** `dotnet build`, `dotnet test` and `npm run build` pass, the behavior is checked in the browser, the work is committed. Never claim a command passed unless it was run. Never cut validation or tests.

## Technical Decisions

- **Entities:** `TaskItem { int Id; string Title; string? Description; TaskItemStatus Status; TaskPriority Priority; DateOnly? DueDate; DateTime CreatedAt; int ProjectId; Project Project = null!; }`. Enums `TaskItemStatus { Todo, InProgress, Done }` and `TaskPriority { Low, Medium, High }` are stored as text (`HasConversion<string>()`). Add `List<TaskItem> Tasks` to `Project`. No `HasMaxLength`. Schema changes go in a new migration in `Data/Migrations/`.
- **Cascade:** `HasMany(p => p.Tasks).WithOne(t => t.Project).HasForeignKey(t => t.ProjectId).IsRequired().OnDelete(DeleteBehavior.Cascade)`. `ProjectService.DeleteAsync` keeps removing only the project row (it does not load tasks), so the database does the work.
- **Dates:** register `TimeProvider.System` as a singleton and inject it into `TaskService`. Today = `DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime)`. `CreatedAt` = `timeProvider.GetUtcNow().UtcDateTime`, mapped with `.HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))` so the JSON ends in `Z`. Never call `DateTime.Now`, `UtcNow` or `Today`.
- **Enum parsing:** `Services/EnumNames.TryParse<TEnum>(string? input, out TEnum value)` accepts only an exact match against `Enum.GetNames<TEnum>()`, ignoring case. Never use `Enum.TryParse`/`Enum.IsDefined` on user input. Request status/priority are `string?`. Output uses the existing global `JsonStringEnumConverter(allowIntegerValues: false)`; a JSON number for status gives the binder's 400 under `$.status`.
- **Validation:** one `TaskService.ValidateAndApplyAsync(request, task, isNew)` shared by POST and PUT. It checks every field (including that the project exists and the due-date rule), collects at most one message per key in a single pass, and copies values onto the entity only when there are no errors. `isNew` only switches the defaults and the due-date comparison. No DataAnnotations validation attributes, no FluentValidation.
- **Exact messages:** "Title is required." · "Title must be at most 200 characters." · "Description must be at most 2000 characters." · "Project is required." · "Project {id} does not exist." · "Project id must be a whole number." · "Status is required." · "Status must be one of: Todo, InProgress, Done." · "Priority is required." · "Priority must be one of: Low, Medium, High." · "Due date cannot be in the past." Keys: `title`, `description`, `projectId`, `status`, `priority`, `dueDate`.
- **Contract:** `GET /api/tasks?projectId=` (status/priority filters arrive in Epic 4), `GET/PUT/DELETE /api/tasks/{id}`, `POST /api/tasks`. POST and PUT share `SaveTaskRequest { title, description, projectId, status, priority, dueDate }` (class, all properties nullable). `TaskDto { id, title, description, status, priority, dueDate, projectId, projectName, createdAt }` is a `record`. `TaskListQuery` is a class with `string?` `{ get; set; }` properties bound with explicit `[FromQuery]`, with `[DisplayFormat(ConvertEmptyStringToNull = false)]`; `ProjectId` is parsed with `int.TryParse` in the service, then checked to exist. Leaving a parameter out means "All".
- **Layers and results:** `TasksController` is HTTP-only (action names without `Async`), maps `ServiceResult` with the inline `switch`, and checks an unknown route id (404) before validating the body on PUT and DELETE.
- **Projection:** one `ToDto` expression in `TaskService`; reads use `.AsNoTracking().Select(ToDto)`, writes save then re-read through it. `ProjectService.ToDto` now computes `TaskCount = p.Tasks.Count()`.
- **Tests (in `Tasks/` and `Persistence/`):** add `FixedTimeProvider` exposed as `factory.Clock` (settable `UtcNow`, `LocalTimeZone` UTC, `Today`) replacing `TimeProvider` in the factory, and a `CreateTaskAsync` HttpClient helper. Build dates from `Clock.Today`; move the clock to test the unchanged-past-date rule. `EnumNames.TryParse` gets its own unit tests. The cascade test deletes via the API, then counts `TaskItems` through a `TaskFlowDbContext` from `factory.Services`. A direct insert with an unknown `ProjectId` must throw `DbUpdateException`. The AC-4 test gives `TaskFlowApiFactory` an optional connection-string constructor parameter, writes through factory A on a temp `.db` file, disposes it, calls `SqliteConnection.ClearAllPools()`, reads through factory B, then deletes the file. Never the EF InMemory provider.

## UX & Interaction Patterns

- **Routes:** `/tasks` → `TasksPage`; `/projects/:id` → `ProjectPage` gains the project task view below its details; `/tasks/new?projectId=` and `/tasks/:id/edit` → `TaskFormPage`, the only place tasks are created or edited (no inline status edits).
- **`TaskTable` component** shows title (a link to the edit page), project name, status, priority, due date (blank when null) and a Delete button. It is shared by both lists. Components take props; only pages import `src/api/`.
- **API module:** `src/api/tasks.ts` with `listTasks(filters: { status?, priority?, projectId? })`. `types.ts` has hand-written `TaskDto` (nullable fields typed `T | null`) and `TASK_STATUSES` / `TASK_PRIORITIES` `as const` arrays; selects render options from them.
- **Form:** labelled Title, Description, Project (preselected from the query), Status (default Todo), Priority (default Medium), Due date. `noValidate`, no `required`/`min`/`max`/`maxLength` (the date input has no `min`, so an unchanged past date can still be saved). Every `''` becomes `null` on submit; `projectId` is sent as a number. Errors appear in `FieldError` next to each input with typed values kept; unmatched keys go to `ErrorBanner`. Dates stay `yyyy-MM-dd` strings, never JS `Date`. Saving returns to the task's (possibly new) project page.
- **Data flow:** pages re-fetch after every change; a failed list load shows `ErrorBanner`; a 404 on `/tasks/:id/edit` renders `NotFoundPage`.
- **Fixed wording:** empty list "No tasks yet."; not found "Task not found."; prompts `Delete task "X"?` and `Delete project "X" and its N tasks?` (N from the latest fetch), via `window.confirm`.

## Cross-Story Dependencies

- Builds on Epic 2: reuse `ServiceResult`, the controller `switch` pattern, `ProjectService.ToDto`, `ProjectPage`, `ErrorBanner`/`FieldError`/`EmptyState`, `client.ts` and `TaskFlowApiFactory`; extend, don't replace.
- 3.1 lays the base for the rest: entity, migration, cascade, `EnumNames`, `TimeProvider`/`FixedTimeProvider`, `CreateTaskAsync`. 3.2 adds the list/ordering, `TaskListQuery` (projectId only) and the UI. 3.3 reuses `ValidateAndApplyAsync` for PUT and the form for editing. 3.4 depends on the cascade from 3.1.
- Epic 4 adds status/priority filters to `TaskListQuery` and `TaskFilters` on both lists, and the dashboard; keep the query class and list pages ready for that.
- Plan: Day 5 = 3.1 (2.5 h, riskiest; if it runs long, move 3.2 to the start of Day 6) + 3.2 (1.5 h); Day 6 = 3.3 (2.5 h) + 3.4 (1.5 h).
