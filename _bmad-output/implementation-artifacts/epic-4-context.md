# Epic 4 Context: Decide what to do next

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Help the user choose what to work on next. They can narrow the task list by status and/or priority, both in the all-tasks list and in a project's task view, and see on the dashboard how many tasks there are in total, how many are completed and how many are pending. The epic proves two release checks: invalid status/priority filter values are rejected (AC-3, filters), and the dashboard totals reflect saved data (AC-6). It builds on the task API, list pages and enum parser from Epic 3.

## Stories

- Story 4.1: Filter tasks by status and priority
- Story 4.2: Dashboard totals

## Requirements & Constraints

- **Filters:** filter by status, by priority or by both. Both together mean a task must match both. Filters combine with `projectId`. Each filter has an "All" option, which means the parameter is left out. Results stay newest first (`CreatedAt` descending, then `Id` descending).
- **Filter validation:** an invalid filter value is a 400, never silently ignored. Status and priority follow the same rule as the request body: only the exact names, ignoring case. Numbers (even `2`), empty values (`?status=`), misspellings and surrounding whitespace are rejected with the same "must be one of" message as the body. An unknown project id is also a 400.
- **Project task view:** offers the same filters. If time runs short, this is cut-line item 2 (after UI polish). Never cut validation or tests.
- **Empty states:** a filtered list with no match shows "No tasks match these filters." An unfiltered empty list still shows "No tasks yet."
- **Dashboard:** Total = all tasks. Completed = `Done`. Pending = `Todo` + `InProgress`, so Total = Completed + Pending. The counts are recalculated from saved data on every request; there are no live updates. With no tasks, all three counts are 0. Counts cover all projects; there are no per-project counts, charts or overdue counts.
- **Performance:** lists, filters and the dashboard must load in under 1 s with 500 tasks. There is no pagination.
- **Accessibility:** filter selects and dashboard numbers are labelled, and everything works with the keyboard.
- **Done means:** `dotnet build`, `dotnet test` and `npm run build` pass, the behavior is checked in the browser, and the work is committed. Never claim a command passed unless it was run.

## Technical Decisions

- **Query binding:** `TaskListQuery` stays a class with `string?` `{ get; set; }` properties (`Status`, `Priority`, `ProjectId`), bound with explicit `[FromQuery]`. Each property has `[DisplayFormat(ConvertEmptyStringToNull = false)]`, so `?status=` arrives as `""` and is rejected instead of being treated as "All". Never make it a positional record, because MVC ignores `[DisplayFormat]` there.
- **Parsing:** status and priority filters go through the existing `EnumNames.TryParse<TEnum>`. Never use `Enum.TryParse`/`Enum.IsDefined` on user input. Collect every query error in one pass (keys `status`, `priority`, `projectId`) and return `ServiceResult.Invalid` so the controller's existing `switch` gives a `ValidationProblemDetails`.
- **Messages:** "Status must be one of: Todo, InProgress, Done." · "Priority must be one of: Low, Medium, High." · "Project {id} does not exist." · "Project id must be a whole number."
- **Dashboard API:** `GET /api/dashboard` → `200 DashboardDto { total, completed, pending }`. `DashboardDto` is a `record` in `Dtos/`. `DashboardController` is HTTP-only and calls one method on a concrete `DashboardService` (registered with `AddScoped`, no interface), which counts from `TaskFlowDbContext` with async EF queries on each request. No repositories or extra layers.
- **Frontend API:** `src/api/dashboard.ts` for the dashboard call. `listTasks(filters: { status?, priority?, projectId? })` in `src/api/tasks.ts` passes only the filters that are set.
- **Tests (`Tasks/`, `Dashboard/`):** integration tests go through `HttpClient` on `TaskFlowApiFactory` (real in-memory SQLite, never EF InMemory), using `CreateProjectAsync`/`CreateTaskAsync` and deserializing into the API's DTOs. Cover: each filter alone, both combined, combined with `projectId`, case-insensitive values, and the 400s for `?status=7`, `?status=2`, `?status=`, `?status=Hgh`, `?priority=7` and `?priority=`, asserting the `errors` key. For the dashboard: all zeros when there are no tasks; a mixed set (Todo, InProgress, Done, Done → 4/2/2); and the next call reflecting each of create, edit to `Done`, delete task and delete project with tasks (AC-6). Test names follow `Method_Scenario_Expected`.

## UX & Interaction Patterns

- **`TaskFilters` component:** two labelled native `<select>`s (Status, Priority) with the options "All" plus `TASK_STATUSES` / `TASK_PRIORITIES`. It takes props and callbacks and never imports `src/api/`.
- **Filter state lives in the URL** via `useSearchParams` (for example `/tasks?status=Todo&priority=High`). Choosing "All" removes that parameter. Reloading and the back button keep the filters. The page reads the params and re-fetches the list when they change.
- **Both list pages use it:** `TasksPage` calls `GET /api/tasks?status=&priority=`, and `ProjectPage` calls `GET /api/tasks?projectId=<id>&status=&priority=`.
- **Dashboard page (`/`):** shows three labelled numbers (Total, Completed, Pending), freshly fetched on every open or reload. If the API is unreachable, it shows the `ErrorBanner` instead of the numbers, never a blank screen. This replaces the walking-skeleton "API connected" placeholder.
- Plain CSS in `src/index.css`; no UI kit and no data-fetching library.

## Cross-Story Dependencies

- Both stories depend on Epic 3: `TaskItem`, `TaskListQuery` (currently `projectId` only), `EnumNames`, `TaskTable`, `TASK_STATUSES`/`TASK_PRIORITIES`, the list pages and the test helpers. Extend them; don't replace them.
- 4.1 and 4.2 are independent of each other. The 4.2 integration tests rely on the task edit/delete and project cascade from Epic 3.
- Plan: Day 7 = 4.1 (1.5 h) + 4.2 (1 h), followed by Story 5.1. Story 5.1 later measures the filters and the dashboard against the 500-task, under-1-second target, and checks the keyboard flow.
