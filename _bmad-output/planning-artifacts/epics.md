---
status: final
stepsCompleted: [1, 2, 3, 4]
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-taskFlow-2026-10-21/prd.md
  - _bmad-output/planning-artifacts/prds/prd-taskFlow-2026-10-21/addendum.md
  - _bmad-output/planning-artifacts/architecture/architecture-taskFlow-2026-10-22/ARCHITECTURE-SPINE.md
  - CLAUDE.md
---

# TaskFlow - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for TaskFlow, decomposing the requirements from the PRD and Architecture requirements into implementable stories. There is no separate UX document; screen and form behavior comes from architecture AD-12.

## Requirements Inventory

### Functional Requirements

- **FR-1 Create a project:**
  - Name is required, 1–100 characters after trimming. Description is optional, up to 1,000 characters.
  - Names do not need to be unique. Values are saved trimmed.
  - Invalid input saves nothing and returns an error for each failing field.
- **FR-2 View projects:**
  - The list shows each project's name and task count, or an empty state when there are none.
  - Opening a project shows its name, description and tasks: the **project task view**, with the same columns and order as FR-6, and an empty state when it has no tasks.
  - An unknown project returns 404, and the UI says so.
- **FR-3 Edit a project:** FR-1 rules apply. Changes persist after a restart. An unknown project returns 404.
- **FR-4 Delete a project and its tasks:**
  - The confirmation states how many tasks will also be deleted.
  - After deletion, the project and its tasks can't be retrieved, and the dashboard no longer counts them.
  - The database enforces the cascade.
- **FR-5 Create a task:**
  - Title is required, 1–200 characters after trimming, and saved trimmed.
  - The project is required and must exist.
  - Status defaults to `Todo` and priority to `Medium`. Both accept only their exact names, ignoring case; numbers, misspellings and empty strings are rejected, in the body and in filters.
  - Due date is optional and must be today or later.
  - Description is optional, up to 2,000 characters.
  - On success the task is returned with its id.
- **FR-6 View tasks:**
  - Each task shows title, project, status, priority and due date.
  - Tasks are listed newest first by creation time.
  - An unknown task returns 404.
- **FR-7 Edit a task:**
  - A full replace: title, project, status and priority are required, with no defaults. An absent or empty description or due date is cleared.
  - FR-5 rules apply.
  - An unchanged past due date is accepted; a new past date is rejected.
  - Changes persist after a restart. An unknown task returns 404.
- **FR-8 Delete a task:** a confirmation is shown. Afterwards the task can't be retrieved and the dashboard updates. An unknown task returns 404.
- **FR-9 Filter tasks:**
  - Filter by status and/or priority (AND), each with an "All" option.
  - The same filters work in the project task view.
  - No matches shows an empty state.
  - Invalid filter values (status, priority, unknown project id) are rejected with 400.
- **FR-10 Dashboard:**
  - Total = all tasks; Completed = `Done`; Pending = `Todo` + `InProgress`. Total = completed + pending.
  - Recalculated from saved data each time the dashboard is opened. All zeros when there are no tasks.
- **FR-11 Useful error messages:**
  - The backend validates every write and returns 400 with one message per failing field, naming the field and the rule.
  - The UI shows each message next to its input and keeps what the user typed.
  - An unreachable backend or an unexpected error shows a general message, never a blank screen.

### NonFunctional Requirements

- **NFR-1 Persistence:** the data lives in a SQLite file and survives a backend restart.
- **NFR-2 Data integrity:** the database enforces that every task has an existing project, and that deleting a project deletes its tasks.
- **NFR-3 Local run:** the README documents the prerequisites and the exact setup, run and test commands. The database is created automatically, with no manual SQL step.
- **NFR-4 Cost:** only free, open-source tools. No paid or cloud services.
- **NFR-5 Testability:** xUnit tests cover every validation rule and the cascade. Integration tests use real SQLite in-memory.
- **NFR-6 API docs:** an OpenAPI/Swagger page in Development.
- **NFR-7 Responsiveness:** lists, filters and the dashboard load in under 1 s with 500 tasks.
- **NFR-8 Usability:** current Chrome, Firefox and Edge. Labelled inputs, and every action works with the keyboard.
- **NFR-9 Code clarity:** readable for a beginner. HTTP, business rules and data access are kept apart, with no needless layers.

### Release Acceptance Checks (PRD §10)

- **AC-1:** A task must have a valid title. (Automated integration tests)
- **AC-2:** A task must reference a valid project. (Automated integration tests)
- **AC-3:** Invalid status and priority are rejected in the body and in filter parameters. (Automated integration tests)
- **AC-4:** Task updates persist after the backend restarts. (Manual check, plus one temp-file SQLite test)
- **AC-5:** Deleting a project deletes all its tasks. (Automated integration tests)
- **AC-6:** Dashboard totals reflect saved data. (Automated integration tests)
- **AC-7:** The app runs locally with the documented commands. (Manual check on a fresh clone)

### Additional Requirements

From the architecture spine (AD-n) and CLAUDE.md:

- **Starter templates (Epic 1 Story 1):**
  - Run `dotnet new sln` (→ `backend/TaskFlow.slnx`), `dotnet new webapi --use-controllers` (→ `backend/TaskFlow.Api`) and `dotnet new xunit` (→ `backend/TaskFlow.Api.Tests`).
  - Run `npm create vite@9.3.1 frontend -- --template react-ts`.
  - Set up the dotnet-ef local tool with `dotnet new tool-manifest -o .config`.
- **Dependencies beyond the templates** (AD Stack): Swashbuckle.AspNetCore.SwaggerUI 10.3.1; EF Core Sqlite and Design 10.0.11; dotnet-ef 10.0.11; Microsoft.AspNetCore.Mvc.Testing 10.0.11; react-router 8.5.x (never react-router-dom).
- **Layering (AD-1, AD-2):**
  - Controllers are HTTP-only, with action names that have no `Async` suffix.
  - Services own the rules and EF queries. No repositories.
  - `ServiceResult<T>` (Ok / NotFound / Invalid) is mapped with an inline `switch`.
  - On PUT and DELETE, a route 404 comes before body validation.
- **Validation (AD-3):**
  - One `ValidateAndApply(Async)` per entity, shared by create and update. It collects every field error in one pass, using the messages from the spine's message table.
  - Request DTO properties are nullable. No DataAnnotations validation attributes and no FluentValidation.
- **Enums (AD-4, AD-5):**
  - Input is a `string?` parsed by one `EnumNames.TryParse`.
  - `TaskListQuery` is a class bound `[FromQuery]`, with `[DisplayFormat(ConvertEmptyStringToNull=false)]` on Status, Priority and ProjectId.
  - `JsonStringEnumConverter(allowIntegerValues:false)` is used for output.
  - The enums are `TaskItemStatus` and `TaskPriority`, stored as text.
- **Entity shapes (AD-5):** nullable `Description` and `DueDate` fields. No `HasMaxLength`.
- **Cascade (AD-6):** a required FK with `OnDelete(Cascade)`. Delete removes only the project row, and the test checks the database directly.
- **Dates (AD-7):**
  - `DateOnly? DueDate`; today comes from `TimeProvider.GetLocalNow()`.
  - `CreatedAt` is a UTC `DateTime` with a `SpecifyKind` converter, so the JSON ends in `Z`.
  - Lists are ordered by CreatedAt descending, then Id descending.
- **PUT semantics (AD-8):** defaults only on create; only null clears; descriptions are trimmed and whitespace becomes null.
- **API contract (AD-9):** the route and DTO table, with camelCase error keys.
- **Migrations (AD-10):** applied on startup (`MigrateAsync`). Connection string `Data Source=taskflow.db`, with the DbContext reading configuration lazily. No seed data.
- **Topology (AD-11):**
  - The API runs at `http://localhost:5080`, with only the http profile, no HTTPS redirection and no CORS.
  - Vite runs on 5173 with `strictPort` and proxies `/api`.
  - Swagger UI is at `/swagger`, Development only.
- **One DTO projection per entity (AD-14):** writes re-read through the same `ToDto` expression.
- **Tests (AD-13):**
  - `TaskFlowApiFactory` gives each test its own shared-cache in-memory SQLite database.
  - `factory.Clock` is a `FixedTimeProvider` in UTC.
  - Shared helpers: `ApiJson.Options`, plus `CreateProjectAsync` and `CreateTaskAsync`.
  - The AC-4 test uses a temp file and `ClearAllPools`. Never the EF InMemory provider.
- **README:** exact prerequisites and commands (AC-7), plus the architecture overview section from `README-architecture-overview.md`.

### UX Design Requirements

There is no UX document. The binding UI behavior comes from architecture AD-12 and the Conventions table:

- **UX-DR1 API client:**
  - `src/api/client.ts` `request<T>()` throws `ApiError { status, message, fieldErrors }`.
  - It parses JSON only when the Content-Type says JSON.
  - A fetch failure or a non-JSON 5xx gives "Cannot reach the server. Is the API running?"
  - Field keys are normalized (`$.` dropped, first letter lowercased).
- **UX-DR2 Forms:**
  - `noValidate`, with no `required`, `min`, `max` or `maxLength`.
  - Every `''` becomes `null` on submit, and `projectId` is sent as a number.
  - `FieldError` sits next to each labelled input, and typed values are kept on error.
  - Keys with no matching input go into `ErrorBanner`.
- **UX-DR3 Pages:**
  - Only pages import `src/api`.
  - Each page re-fetches after every change.
  - A 404 on the page's own resource renders `NotFoundPage`.
  - Empty lists show `EmptyState` with the fixed wording.
- **UX-DR4 Filters:** live in the URL query (`useSearchParams`); "All" leaves the parameter out. Options come from the `TASK_STATUSES` and `TASK_PRIORITIES` arrays.
- **UX-DR5 Confirmations:**
  - Deletes use `window.confirm`.
  - The project prompt reads: Delete project "X" and its N tasks?
  - Tasks are edited only through `TaskFormPage`.
- **UX-DR6 Accessibility:** every input has a `<label htmlFor>`, and only native interactive elements are used. Plain CSS in `index.css`.

### FR Coverage Map

| Requirement | Epic(s) | Note |
| --- | --- | --- |
| FR-1 | Epic 2 | Create project |
| FR-2 | Epic 2, Epic 3 | List/detail in 2; project task view in 3 |
| FR-3 | Epic 2 | Edit project |
| FR-4 | Epic 2, Epic 3 | Delete + confirm in 2; DB cascade + count prompt with tasks in 3 |
| FR-5 | Epic 3 | Create task |
| FR-6 | Epic 3 | View tasks, newest first |
| FR-7 | Epic 3 | Edit task (full replace, due-date rule) |
| FR-8 | Epic 3 | Delete task |
| FR-9 | Epic 4 | Filters (all tasks + project task view) |
| FR-10 | Epic 4 | Dashboard |
| FR-11 | Epic 1, Epic 2, Epic 3 | Error plumbing in 1; field errors in 2 and 3 |
| NFR-1..NFR-9 | see story ACs | NFR-3/6 Epic 1; NFR-2/5 Epics 2–3; NFR-1 Epic 5; NFR-7/8 Epic 5 |
| AC-1, AC-2 | Epic 3 | |
| AC-3 | Epic 3 (body), Epic 4 (filters) | |
| AC-4 | Epic 5 | |
| AC-5 | Epic 3 | |
| AC-6 | Epic 4 | |
| AC-7 | Epic 1 (README works), Epic 5 (fresh-clone check) | |

## Epic List

### Epic 1: Run TaskFlow locally (walking skeleton)
A reviewer can clone the repo, follow the README, start API + frontend, see the frontend reach the API, open Swagger, and run `dotnet test`. Establishes the patterns every later story reuses.
**Covers:** NFR-3, NFR-6, AC-7 (first pass), FR-11 (error plumbing).

### Epic 2: Organize work into projects
The user can create, list (with task counts), open, edit and delete projects, with field-level validation messages.
**FRs covered:** FR-1, FR-2 (list + detail), FR-3, FR-4 (delete + confirm), FR-11.

### Epic 3: Track tasks inside projects
The user can add tasks to a project, see them newest first in the all-tasks list and the project task view, edit them (incl. moving projects, due-date rules) and delete them; deleting a project removes its tasks via the database cascade.
**FRs covered:** FR-2 (project task view), FR-4 (cascade), FR-5, FR-6, FR-7, FR-8, FR-11. **ACs:** AC-1, AC-2, AC-3 (body), AC-5.

### Epic 4: Decide what to do next
The user can filter tasks by status and priority (all tasks and project task view) and see total / completed / pending on the dashboard.
**FRs covered:** FR-9, FR-10. **ACs:** AC-3 (filters), AC-6.

### Epic 5: Portfolio-ready release
A reviewer can trust the data survives restarts and the README works on a fresh clone; the UI is tidy and keyboard-friendly.
**Covers:** AC-4, AC-7 (final), NFR-1, NFR-7, NFR-8.

## Day Plan (Days 3–7, ≤ 4 h/day)

| Day | Stories | Planned |
| --- | --- | --- |
| Day 3 | 1.1 (+ 1.5 h buffer for setup problems) | 2.5 h + 1.5 h |
| Day 4 | 2.1, 2.2 | 2.75 h + 1.25 h = 4 h |
| Day 5 | 3.1, 3.2 | 2.5 h + 1.5 h = 4 h |
| Day 6 | 3.3, 3.4 | 2.5 h + 1.5 h = 4 h |
| Day 7 | 4.1, 4.2, 5.1 | 1.5 h + 1 h + 1.5 h = 4 h |
| Any spare time | ✂️ 5.2 | 0–1 h, only from unused Day 3 buffer |

**Cut line if behind schedule.** Cut in this order: (1) Story 5.2 UI polish; (2) the filters inside the project task view (FR-9 bullet 3, part of 4.1), which the PRD allows. **Never cut validation, tests or Story 5.1.** If Day 3 runs over, the buffer absorbs it. Planned work: 18.5 h + 1.5 h buffer = 20 h. Story 3.1 is the riskiest sitting; if it runs long, finish it on Day 5 and move 3.2 to the start of Day 6. If any day runs over by more than 1 h, apply the cut line, don't stretch the day.

**Definition of Done for every story (PRD §9):** acceptance criteria met; `dotnet build` and `dotnet test` pass; `npm run build` passes; new rules have tests; the behavior has been checked in the browser; committed with a clear message; only free, open-source tools and no paid or cloud services (NFR-4); code follows the spine layering (Controllers → Services → DbContext), with no extra abstractions (NFR-9). Run all commands before claiming they pass (CLAUDE.md).

## Epic 1: Run TaskFlow locally (walking skeleton)

A reviewer can clone the repo, follow the README, start the API and the frontend, see the frontend reach the API, open Swagger, and run `dotnet test`. This epic sets the patterns every later story reuses.

### Story 1.1: Walking skeleton — clone, run, see the API respond

As a portfolio reviewer,
I want to start the backend and frontend with the README commands and see them talk to each other,
So that I know the project runs locally before any features exist.

**Day / estimate:** Day 3, 2.5 h. **Refs:** NFR-3, NFR-4, NFR-6, NFR-9, FR-11 (general error), AC-7 (first pass); AD-1, AD-9, AD-11, AD-12, AD-13; Stack; Structural Seed (scaffolding commands).

**Acceptance Criteria:**

**Given** a clone with the .NET 10 SDK and Node.js 22.22+ installed
**When** I run `cd backend && dotnet run --project TaskFlow.Api`
**Then** the API listens on `http://localhost:5080`, and `GET /api/health` returns 200 with the body `Healthy`
**And** `launchSettings.json` has only the `http` profile, there is no `UseHttpsRedirection` and no CORS policy, and `Program.cs` registers `JsonStringEnumConverter(allowIntegerValues: false)`, `AddProblemDetails()` + `UseExceptionHandler()`, and health checks.

**Given** the API is running in Development
**When** I open `http://localhost:5080/swagger`
**Then** Swagger UI loads and shows the document from `/openapi/v1.json`.

**Given** the API is running
**When** I run `cd frontend && npm install && npm run dev` and open `http://localhost:5173`
**Then** I see a layout with keyboard-reachable nav links (Dashboard, Projects, Tasks), and the Dashboard page shows "API connected" after calling `/api/health` through the Vite proxy
**And** `vite.config.ts` uses port 5173 with `strictPort: true` and proxies `/api` to `http://localhost:5080`.

**Given** the frontend is running and the API is stopped
**When** I reload the Dashboard page
**Then** an `ErrorBanner` shows "Cannot reach the server. Is the API running?", not a blank screen
**And** this comes from `src/api/client.ts` `request<T>()` throwing `ApiError { status: 0, … }`. The client parses JSON only when the Content-Type says JSON, and normalizes error keys (`$.` dropped, first letter lowercased)
**And** a non-2xx JSON response (e.g. a 500 `ProblemDetails`) throws an `ApiError` whose `message` is the body's `title`, which is shown in the `ErrorBanner` (FR-11, unexpected error).

**Given** the app is running
**When** I visit `/projects`, `/tasks` and `/does-not-exist`
**Then** `/projects` and `/tasks` show placeholder headings, and the unknown URL shows `NotFoundPage`
**And** routing uses `BrowserRouter` imported from `react-router` (8.x); `react-router-dom` is not installed.

**Given** the backend folder
**When** I run `dotnet test`
**Then** a `Health_Get_ReturnsHealthy` test passes. It uses `TestInfrastructure/TaskFlowApiFactory : WebApplicationFactory<Program>` and `HttpClient`.

**Given** the repository
**When** I inspect it
**Then** the folders match the spine's Structural Seed (`backend/TaskFlow.slnx`, `backend/TaskFlow.Api`, `backend/TaskFlow.Api.Tests`, `frontend/`, `.config/dotnet-tools.json` with dotnet-ef 10.0.11)
**And** the only packages beyond the templates are Swashbuckle.AspNetCore.SwaggerUI 10.3.1, Microsoft.AspNetCore.Mvc.Testing 10.0.11 and react-router 8.x. All Microsoft packages are on 10.0.11.

**Given** the root `README.md`
**When** I follow it on this machine
**Then** it lists the prerequisites and the exact run, test and build commands from the spine (the developer-only `dotnet ef migrations add` line and the "creates taskflow.db" note are added in Story 2.1), plus the architecture overview section (copied from `README-architecture-overview.md`). Every command was actually run and worked
**And** `npm run build` passes.

## Epic 2: Organize work into projects

The user can create, list (with task counts), open, edit and delete projects, with field-level validation messages.

### Story 2.1: Create a project and see it in the list

As the user,
I want to create a project and see all my projects listed,
So that I can group related tasks.

**Day / estimate:** Day 4, 2.75 h. **Refs:** FR-1, FR-2 (list, get by id), FR-11, NFR-1, NFR-3, NFR-5; AD-2, AD-3, AD-5, AD-8, AD-9, AD-10, AD-12, AD-13, AD-14. Introduces the database, the result type, the validation pattern and the form-error pattern.

**Acceptance Criteria:**

**Given** no `taskflow.db` exists
**When** I start the API
**Then** the startup `MigrateAsync()` applies the first migration (in `Data/Migrations/`), creating `taskflow.db` with a `Projects` table. There is no manual SQL or EF command
**And** `Project { Id, Name, Description? }` and `TaskFlowDbContext` exist; EF Core Sqlite and Design 10.0.11 are added; the DbContext reads `ConnectionStrings:TaskFlow` lazily (AD-10).

**Given** a valid request `{ "name": "  Portfolio site  ", "description": "  " }`
**When** I `POST /api/projects`
**Then** I get 201 with a `Location` header and `ProjectDto { id, name: "Portfolio site", description: null, taskCount: 0 }`. Name and description are trimmed, and a whitespace-only description becomes null.

**Given** an invalid request
**When** I `POST /api/projects`
**Then** I get 400 `ValidationProblemDetails` and nothing is saved:
- `name` missing, empty or whitespace-only → `errors.name` = "Name is required."
- `name` longer than 100 characters after trimming → "Name must be at most 100 characters."
- `description` longer than 1,000 characters → `errors.description` = "Description must be at most 1000 characters."

**And** a request that breaks both rules returns both keys in one response (single pass, AD-3)
**And** a name of exactly 100 characters with surrounding spaces is accepted
**And** `"description": "  My notes  "` is saved as "My notes".

**Given** two projects with the same name
**When** I create the second one
**Then** it is accepted (names need not be unique).

**Given** projects "beta", "Alpha" and "alpha" exist
**When** I `GET /api/projects`
**Then** I get 200 with all of them ordered by name, then id, each with `taskCount`. Until Story 3.1 adds `TaskItem`, `ToDto` sets `TaskCount = 0`
**And** reads use `AsNoTracking().Select(ToDto)`, the POST response is re-read through the same `ToDto` (AD-14), and the controller maps `ServiceResult` with an inline `switch` (AD-2)
**And** `GET /api/projects/{id}` returns 200 `ProjectDto`, or 404 `ProblemDetails` for an unknown id. The POST's `Location` header points to it (`CreatedAtAction(nameof(GetById), …)`, AD-2).

**Given** the Projects page with no projects
**When** it loads
**Then** it shows "No projects yet. Create your first project." and a create form with labelled Name and Description inputs (`noValidate`, no HTML constraint attributes).

**Given** I submit the form with an empty name
**When** the API returns 400
**Then** "Name is required." appears next to the Name input, what I typed stays in the fields, and any error key with no input appears in the `ErrorBanner`
**And** on a successful create the form clears and the list is re-fetched, showing the new project with its task count
**And** if loading the list fails, the page shows the `ErrorBanner` instead of the list.

**Given** the test project
**When** I run `dotnet test`
**Then** integration tests cover every FR-1 rule above through `HttpClient`, each on its own in-memory SQLite database (`Data Source=taskflow-test-{guid};Mode=Memory;Cache=Shared`, kept open by the factory and injected with `UseSetting`)
**And** `ApiJson.Options` and the `CreateProjectAsync` helper exist in `TestInfrastructure/`, and get-by-id 200/404 is tested
**And** the README gains the `dotnet tool restore && dotnet ef migrations add …` command (used to create this migration) and the "creates taskflow.db" note
**And** restarting the API keeps the created projects (manual check).

### Story 2.2: Open, edit and delete a project

As the user,
I want to open a project, change its name or description, and delete it,
So that my project list stays accurate.

**Day / estimate:** Day 4, 1.25 h. **Refs:** FR-2 (detail page, 404 in UI), FR-3, FR-4 (delete + confirmation), FR-11; AD-2, AD-3, AD-8, AD-12, AD-14.

**Acceptance Criteria:**

**Given** a project exists
**When** I `PUT /api/projects/{id}` with a valid body
**Then** I get 200 with the updated `ProjectDto`, re-read through `ToDto`, so `taskCount` is correct
**And** the update uses the same `ValidateAndApply` method as create, so every FR-1 rule and message from Story 2.1 applies (tested for an empty name and a 101-character name)
**And** it is a full replace: an omitted description is cleared.

**Given** an unknown id and an invalid body
**When** I `PUT /api/projects/{id}`
**Then** I get 404, not 400 (the route id is checked first, AD-2).

**Given** a project exists
**When** I `DELETE /api/projects/{id}`
**Then** I get 204, and a following `GET` returns 404. Deleting an unknown id returns 404.

**Given** the Projects page
**When** I click (or press Enter on) a project name
**Then** `/projects/:id` shows the project's name, description and task count, with Edit and Delete buttons.

**Given** I edit the project and submit an invalid name
**When** the API returns 400
**Then** the message appears next to the Name input and my input is kept. A valid submit shows the updated values, re-fetched.

**Given** I click Delete
**When** the browser asks "Delete project "X" and its N tasks?" (N = `taskCount` from the latest fetch)
**Then** OK deletes the project and returns me to `/projects` without it, and Cancel changes nothing.

**Given** I open `/projects/9999`
**When** the API returns 404
**Then** the page renders `NotFoundPage` with "Project not found."

**Given** I edited a project
**When** I restart the API and reload
**Then** the change is still there (manual check, NFR-1)
**And** `dotnet test` covers update success, update validation, update 404-before-400, delete 204 then 404, and delete-unknown 404.

## Epic 3: Track tasks inside projects

The user can add tasks to a project, see them newest first in the all-tasks list and the project task view, edit them (including moving them between projects and the due-date rules), and delete them. Deleting a project removes its tasks through the database cascade.

### Story 3.1: Add a task to a project (API and rules)

As the user,
I want the system to accept a task with a title, status, priority and optional due date for a project, and reject bad input,
So that I can track the work.

**Day / estimate:** Day 5, 2.5 h. **Refs:** FR-5, FR-11, NFR-2, NFR-5; AC-1, AC-2, AC-3 (body); AD-3, AD-4, AD-5, AD-6, AD-7, AD-8, AD-12, AD-13, AD-14. Introduces `TaskItem`, the cascade relationship, `EnumNames`, `TimeProvider` and `FixedTimeProvider`.

**Acceptance Criteria:**

**Given** the API starts on an existing database
**When** the new migration is applied on startup
**Then** a `TaskItems` table exists with a required `ProjectId` foreign key and `ON DELETE CASCADE`. Status and priority are stored as text, and `CreatedAt` is read back as UTC (the `SpecifyKind` converter)
**And** the entities and enums match AD-5 exactly (`TaskItem`, `TaskItemStatus`, `TaskPriority`, with nullable `Description` and `DueDate`)
**And** `Project.Tasks` is added, and `ProjectService.ToDto` now computes `TaskCount = p.Tasks.Count()`
**And** `FixedTimeProvider` (exposed as `factory.Clock`, UTC, settable) and the `CreateTaskAsync` helper are added to `TestInfrastructure/`.

**Given** an existing project
**When** I `POST /api/tasks` with `{ "title": "  Write README  ", "projectId": <id> }`
**Then** I get 201 with `TaskDto`: title "Write README", status `Todo`, priority `Medium`, dueDate null, the correct `projectName`, and a `createdAt` ending in `Z`
**And** `GET /api/tasks/{id}` returns the same DTO, `GET /api/tasks/9999` returns 404, and the project's `taskCount` is now 1 in `GET /api/projects`.

**Given** a task body with a bad title (AC-1)
**When** I POST it
**Then** a title that is missing, empty or whitespace-only gives 400 `errors.title` "Title is required.", and a title longer than 200 characters after trimming gives "Title must be at most 200 characters." Nothing is saved.

**Given** a task body with a bad project (AC-2)
**When** I POST it
**Then** a missing `projectId` gives `errors.projectId` "Project is required.", and an unknown id gives "Project 9999 does not exist."

**Given** status or priority values (AC-3, body)
**When** I POST them
**Then** `"high"`, `"HIGH"` and `"High"` are all accepted and returned as `"High"`
**And** each of `"7"`, `"2"`, `""`, `" Todo"`, `"Todo,Done"` and `"Hgh"` gives 400 with "Status must be one of: Todo, InProgress, Done." (or the priority equivalent)
**And** a JSON number (`"status": 2`) gives 400 under the key `$.status`
**And** `EnumNames.TryParse` has its own unit tests.

**Given** `factory.Clock` is set to a fixed date
**When** I POST due dates
**Then** yesterday gives 400 `errors.dueDate` "Due date cannot be in the past.", while today, tomorrow and null are accepted.

**Given** a body with an empty title, an unknown project and status `"7"`
**When** I POST it
**Then** all three keys come back in one 400 response (single pass), and a description longer than 2,000 characters adds `errors.description`.

**Given** the API is running
**When** I create a task through Swagger UI
**Then** the project's task count shown on the Projects page and the project page has increased (manual check).

### Story 3.2: Add tasks from the UI and see them newest first

As the user,
I want to add tasks from a form and see all my tasks and each project's tasks, newest first,
So that I can capture work quickly and find what I'm working on.

**Day / estimate:** Day 5, 1.5 h. **Refs:** FR-5 (UI), FR-11, FR-2 (project task view), FR-6, FR-9 (projectId filter validation); AD-4, AD-7, AD-9, AD-12, AD-14.

**Acceptance Criteria:**

**Given** tasks created at different `factory.Clock` times
**When** I `GET /api/tasks`
**Then** all tasks are returned ordered by `createdAt` descending, then id descending
**And** two tasks with the same `createdAt` are ordered by id descending.

**Given** two projects with tasks
**When** I `GET /api/tasks?projectId=<id>`
**Then** only that project's tasks are returned, newest first
**And** `?projectId=9999` gives 400 `errors.projectId` "Project 9999 does not exist.", while `?projectId=abc` and `?projectId=` give 400 "Project id must be a whole number."
**And** `TaskListQuery` is a class with `{ get; set; }` properties, bound with `[FromQuery]`, with `[DisplayFormat(ConvertEmptyStringToNull = false)]` on `ProjectId` (AD-4).

**Given** the Tasks page (`/tasks`)
**When** it loads
**Then** a `TaskTable` shows each task's title, project name, status, priority and due date (blank when there is none), newest first
**And** with no tasks it shows "No tasks yet."

**Given** a project page
**When** it loads
**Then** below the project details, the same `TaskTable` shows only that project's tasks (the project task view), and an empty project shows "No tasks yet."
**And** if loading a list fails, the page shows the `ErrorBanner` instead of the list.

**Given** the project page
**When** I click "Add task"
**Then** `/tasks/new?projectId=<id>` shows a form with labelled Title, Description, Project (preselected), Status (default Todo), Priority (default Medium) and Due date inputs. The options come from `TASK_STATUSES` and `TASK_PRIORITIES`, and the date input has no `min`.

**Given** I submit the form with an empty title and yesterday's date
**When** the API returns 400
**Then** both messages appear next to their inputs, and my values are kept. Empty inputs are sent as `null` and `projectId` as a number
**And** a valid submit returns me to the project page, where the new task appears at the top of the project task view and the task count has increased.


### Story 3.3: Edit a task (including persistence after restart)

As the user,
I want to change any field of a task, including its status and its project,
So that my tasks reflect reality.

**Day / estimate:** Day 6, 2.5 h. **Refs:** FR-7, FR-11, NFR-1; AC-1, AC-2, AC-3 (on update), **AC-4**; AD-2, AD-3, AD-7, AD-8, AD-13.

**Acceptance Criteria:**

**Given** a task exists
**When** I `PUT /api/tasks/{id}` with every field
**Then** I get 200 with the updated `TaskDto`, re-read through `ToDto`
**And** changing `projectId` to another existing project moves the task: both projects' `taskCount` values update, and `projectName` changes.

**Given** a PUT body missing a required field
**When** I send it
**Then** a null status gives "Status is required.", a null priority gives "Priority is required.", a null title gives "Title is required." and a null projectId gives "Project is required." (no defaults on edit)
**And** an omitted, null, empty or whitespace-only description, and an omitted or null due date, clear the stored value. In the UI an emptied due-date input is sent as `null` (AD-12); a raw `"dueDate": ""` sent straight to the API is a 400 under `$.dueDate` (AD-3)
**And** a whitespace title (AC-1), an unknown projectId (AC-2) and status `"7"` (AC-3) are each rejected exactly as on create, through the same `ValidateAndApplyAsync`.

**Given** a task created with due date = today, and `factory.Clock` then moved forward 2 days
**When** I PUT the task with the same due date
**Then** it is accepted (an unchanged past date is allowed)
**And** a different past date gives 400 "Due date cannot be in the past.", while null clears the date and a future date is accepted.

**Given** an unknown task id
**When** I PUT, even with an invalid body
**Then** I get 404.

**Given** the task lists
**When** I click (or press Enter on) a task title
**Then** `/tasks/:id/edit` opens the form pre-filled with every field. Saving returns me to the task's (possibly new) project page
**And** validation errors appear next to the inputs and my input is kept
**And** `/tasks/9999/edit` renders `NotFoundPage` with "Task not found."

**Given** the AC-4 persistence test in `Persistence/`
**When** it runs
**Then** `TaskFlowApiFactory` takes an optional connection-string constructor parameter, and the test creates a project and a task on a temporary `.db` file through factory A, updates the task, disposes factory A, calls `SqliteConnection.ClearAllPools()`, starts factory B on the same file, and reads back the updated values. The temp file is deleted afterwards.

**Given** the running app
**When** I edit a task in the browser, stop the API, start it again and reload
**Then** the edit is still there. This manual AC-4 check is done and noted in the story's completion notes.

### Story 3.4: Delete a task, and prove the project cascade

As the user,
I want to delete a single task, and have a project's tasks disappear when I delete the project,
So that finished work does not clutter my lists.

**Day / estimate:** Day 6, 1.5 h. **Refs:** FR-4 (cascade + count), FR-8, NFR-2; **AC-5**; AD-6, AD-12, AD-13.

**Acceptance Criteria:**

**Given** a task exists
**When** I `DELETE /api/tasks/{id}`
**Then** I get 204, a following `GET` returns 404, and the project's `taskCount` decreases. An unknown id returns 404.

**Given** a project with 3 tasks and another project with 1 task (AC-5)
**When** I `DELETE /api/projects/{id}` for the first project
**Then** I get 204. A `TaskFlowDbContext` from `factory.Services` counts 0 `TaskItems` with that `ProjectId`, and `GET /api/tasks/{id}` returns 404 for each of the 3 tasks
**And** the other project's task is untouched
**And** `ProjectService.DeleteAsync` removes only the project row, without loading its tasks (AD-6).

**Given** the database
**When** a test inserts a `TaskItem` with a `ProjectId` that doesn't exist, directly through the DbContext
**Then** `SaveChangesAsync` throws a `DbUpdateException` (a foreign-key violation): the database itself enforces that a task has an existing project (NFR-2).

**Given** the Tasks page or a project page
**When** I click a task's Delete button and confirm "Delete task "X"?"
**Then** the task disappears after a re-fetch, and Cancel changes nothing.

**Given** a project with 2 tasks
**When** I click Delete on its project page
**Then** the prompt reads "Delete project "X" and its 2 tasks?". After OK, `/tasks` no longer lists those tasks.

## Epic 4: Decide what to do next

The user can filter tasks by status and priority (in the all-tasks list and the project task view) and see total, completed and pending on the dashboard.

### Story 4.1: Filter tasks by status and priority

As the user,
I want to narrow the task list by status and/or priority,
So that I can see what to work on next.

**Day / estimate:** Day 7, 1.5 h. **Refs:** FR-9; **AC-3 (filters)**; AD-4, AD-12. ✂️ The filters inside the project task view are cut-line item 2.

**Acceptance Criteria:**

**Given** tasks with mixed statuses and priorities in two projects
**When** I `GET /api/tasks?status=todo&priority=HIGH`
**Then** only tasks that are both `Todo` and `High` are returned, newest first. Each filter alone works, they combine with `projectId`, and leaving out a parameter means "All".

**Given** invalid filter values (AC-3, filters)
**When** I `GET /api/tasks?status=7`, `?status=2`, `?status=`, `?status=Hgh`, `?priority=7` or `?priority=`
**Then** each returns 400 with `errors.status` (or `errors.priority`) carrying the same "must be one of" message as the body. The value goes through `EnumNames.TryParse`, and `TaskListQuery.Status` and `Priority` have `[DisplayFormat(ConvertEmptyStringToNull = false)]`.

**Given** the Tasks page
**When** I choose Status = Todo and Priority = High in the labelled `TaskFilters` selects (options: All + `TASK_STATUSES` / `TASK_PRIORITIES`)
**Then** the URL becomes `/tasks?status=Todo&priority=High`, the list shows only matching tasks, and choosing "All" removes that parameter
**And** reloading the page or using the back button keeps the filters
**And** when nothing matches, the page shows "No tasks match these filters."

**Given** a project page
**When** I use the same `TaskFilters`
**Then** the project task view is filtered the same way (`/api/tasks?projectId=<id>&status=…&priority=…`), and shows "No tasks match these filters." when nothing matches.

### Story 4.2: Dashboard totals

As the user,
I want to see total, completed and pending task counts,
So that I know how much work is left.

**Day / estimate:** Day 7, 1 h. **Refs:** FR-10; **AC-6**; AD-1, AD-9, AD-12.

**Acceptance Criteria:**

**Given** no tasks
**When** I `GET /api/dashboard`
**Then** I get `{ "total": 0, "completed": 0, "pending": 0 }`.

**Given** tasks with statuses Todo, InProgress, Done and Done
**When** I `GET /api/dashboard`
**Then** I get total 4, completed 2, pending 2 (pending = Todo + InProgress, so total = completed + pending)
**And** after creating a task, editing a task to `Done`, deleting a task, or deleting a project with tasks, the next call reflects the change (AC-6). Each case is an integration test.

**Given** the Dashboard page (`/`)
**When** I open or reload it
**Then** it shows three labelled numbers (Total, Completed, Pending), freshly fetched from `/api/dashboard`. If the API is unreachable, it shows the `ErrorBanner` instead of numbers.

## Epic 5: Portfolio-ready release

A reviewer can trust that the data survives restarts and that the README works on a fresh clone. The UI is tidy and keyboard-friendly.

### Story 5.1: Release check — fresh clone, all acceptance checks, performance

As a portfolio reviewer,
I want the README to work on a fresh clone and every release check to be proven,
So that I can trust the project.

**Day / estimate:** Day 7, 1.5 h. **Refs:** **AC-7** (final), AC-1…AC-6 summary, NFR-3, NFR-6, NFR-7, NFR-8; SM-1, SM-2, SM-3. **Never cut.**

**Acceptance Criteria:**

**Given** a fresh `git clone` into an empty folder
**When** I follow only the README
**Then** the API and the frontend start, I can create a project and a task, `dotnet test` passes and `npm run build` passes, all within 10 minutes (SM-2). The time taken is recorded.

**Given** the release checks AC-1 to AC-7
**When** I review them
**Then** a checklist in the story's completion notes names, for each check, the test(s) or manual step that proves it and its result (pass or fail). The manual AC-4 restart check is repeated on the fresh clone.

**Given** 500 tasks created through the API (a throwaway `curl` loop, not committed; no new dependency)
**When** I open the Tasks page, apply a filter and open the Dashboard
**Then** each loads in under 1 second, as measured in the browser's network panel (NFR-7).

**Given** Chrome and Firefox (plus Edge if available)
**When** I complete UJ-1 (plan the week) and UJ-2 (clean up) using only the keyboard
**Then** every action works, and every input has a visible label (NFR-8).

**Given** the running API
**When** I open `/swagger`
**Then** every endpoint in the AD-9 table is listed (NFR-6).

### Story 5.2: UI polish and keyboard pass ✂️ (first to cut)

As the user,
I want a tidy, readable interface with visible focus,
So that the app feels finished.

**Day / estimate:** 0–1 h, only from unused buffer time after 5.1. **Refs:** NFR-8. ✂️ Cut-line item 1.

**Acceptance Criteria:**

**Given** the existing pages
**When** the polish is applied
**Then** plain CSS in `src/index.css` gives consistent spacing, readable tables, and a visible focus outline on every interactive element
**And** statuses display as "To do", "In progress" and "Done" (display only; the values sent to the API stay unchanged).

**Given** the polish changes
**When** I run `dotnet test` and `npm run build`
**Then** both pass, no behavior or API calls changed, and no new dependency or UI library was added.
