# Epic 2 Context: Organize work into projects

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Let the user create, list (with task counts), open, edit and delete projects, with a validation message next to each failing field. Projects are the containers every task will belong to, so this epic also lays the foundations later epics reuse: the SQLite database with migrations applied at startup, the `ServiceResult` type that services return to controllers, the single-pass validation pattern, the one-DTO-projection rule, the frontend form-error pattern, and the in-memory SQLite test setup.

## Stories

- Story 2.1: Create a project and see it in the list
- Story 2.2: Open, edit and delete a project

## Requirements & Constraints

- **Project rules:** name is required, 1–100 characters after trimming. Description is optional, at most 1,000 characters. Names don't have to be unique. Values are saved trimmed, and a whitespace-only description is stored as null.
- **Invalid input** saves nothing and returns 400 with one message for each failing field, all in one response.
- **List:** every project with its name and task count, ordered by name, then id. With no projects, show an empty state.
- **Detail:** shows name, description and task count. An unknown id returns 404 from the API, and the UI renders a not-found page.
- **Edit** is a full replace with the same rules as create (an omitted description is cleared). An unknown id returns 404.
- **Delete** asks for confirmation first. Afterwards the project can't be retrieved. An unknown id returns 404.
- **Persistence:** data lives in a SQLite file, survives an API restart (checked by hand), and the database is created automatically with no manual SQL step.
- **Tests:** xUnit integration tests cover every project rule through `HttpClient`, plus get-by-id 200/404, update success/validation/404-before-400, delete 204-then-404 and delete-unknown 404.
- **Done means:** `dotnet build`, `dotnet test` and `npm run build` pass, the behavior has been checked in the browser, and the work is committed. Never claim a command passed unless it was actually run.

## Technical Decisions

- **Layers:** `ProjectsController` handles HTTP only (binds input, calls one service method, maps the result). It has no rules and no DbContext, and its action names have no `Async` suffix. `ProjectService` is a concrete class registered with `AddScoped`, with no interface. It owns trimming, validation and every EF query. No repositories, AutoMapper or base controllers.
- **`Services/ServiceResult.cs`:** `ResultKind { Ok, NotFound, Invalid }` and `ServiceResult<T>` with `Kind`, `Value`, `Errors` (`Dictionary<string, string[]>`) and the factories `Ok`/`NotFound`/`Invalid`, plus a non-generic `ServiceResult` for delete. Controllers map it with an inline `switch`: Invalid → `ValidationProblem(new ValidationProblemDetails(errors))`; NotFound → `NotFound()`; Ok → `Ok`, `CreatedAtAction(nameof(GetById), new { id }, dto)` or `NoContent()`. On PUT and DELETE, an unknown route id returns 404 before the body is validated. Exceptions are only for unexpected 500s.
- **Validation:** one `ProjectService.ValidateAndApply(request, project)`, shared by create and update. It collects every error in one pass (at most one message per key), checks lengths after trimming, and copies values onto the entity only when there are no errors. No DataAnnotations validation attributes and no FluentValidation. `SaveProjectRequest` is a class with nullable `{ get; set; }` properties. The `[FromBody]` parameter itself stays non-nullable.
- **Exact messages:** "Name is required." · "Name must be at most 100 characters." · "Description must be at most 1000 characters." Error keys are camelCase (`name`, `description`).
- **Contract:** `GET/POST /api/projects`, `GET/PUT/DELETE /api/projects/{id}`. `ProjectDto { id, name, description, taskCount }` is a `record`. 400 responses are `ValidationProblemDetails`; 404 responses are `ProblemDetails`.
- **Entity:** `Project { int Id; string Name; string? Description }`, with an int identity key and no `HasMaxLength`. Story 3.1 adds `Tasks` and the cascade. Until then, `ToDto` sets `TaskCount = 0`.
- **Projection:** one `private static readonly Expression<Func<Project, ProjectDto>> ToDto`. Every read uses `.AsNoTracking().Select(ToDto)`, and POST/PUT save and then re-read by id through the same `ToDto`. Never map entities to DTOs by hand.
- **Database:** add EF Core Sqlite and Design 10.0.11 (Design with `PrivateAssets=all`). `TaskFlowDbContext` lives in `Data/`, migrations in `Data/Migrations/` (created with the repo-local `dotnet-ef`). Register it to read configuration lazily: `AddDbContext<TaskFlowDbContext>((sp, o) => o.UseSqlite(sp.GetRequiredService<IConfiguration>().GetConnectionString("TaskFlow")))`. `appsettings.json` has `ConnectionStrings:TaskFlow` = `Data Source=taskflow.db`. `Program.cs` runs `await db.Database.MigrateAsync()` in a scope after `Build()`. No seed data, no `EnsureCreated`. `*.db`, `*.db-shm` and `*.db-wal` are gitignored. All EF calls are async and awaited.
- **Tests:** `TaskFlowApiFactory` gives each test its own shared-cache in-memory SQLite database (`Data Source=taskflow-test-{guid};Mode=Memory;Cache=Shared`). It keeps one connection open for its lifetime and injects the connection string with `UseSetting("ConnectionStrings:TaskFlow", …)`. Each test class creates a new factory in its constructor and disposes it. Add `TestInfrastructure/ApiJson.Options` (web defaults plus `JsonStringEnumConverter`) and a `CreateProjectAsync` HttpClient extension. Tests go in `Projects/`, deserialize into the API's DTO types, assert status codes and `errors` keys, and are named `Method_Scenario_Expected`. Never use the EF InMemory provider.
- **README:** add the developer-only `dotnet tool restore && dotnet ef migrations add <Name> --project backend/TaskFlow.Api --output-dir Data/Migrations` line and a note that the run command creates `taskflow.db`.

## UX & Interaction Patterns

- **Routes:** `/projects` → `ProjectsPage` (list plus create form); `/projects/:id` → `ProjectPage` (details with Edit and Delete). Project names in the list are links, so they work with Enter.
- **API calls:** add `src/api/projects.ts` and the hand-written `ProjectDto` in `types.ts` (nullable fields typed `T | null`). Only pages import `src/api/`. Components such as `ProjectForm`, `FieldError` and `EmptyState` take props and callbacks.
- **Forms:** use `noValidate` and no `required`/`maxLength` attributes. Every `''` becomes `null` on submit. Each input has a `<label htmlFor>` with a `FieldError` next to it, and the user's typed values are kept on error. Error keys with no matching input, and the error's `message`, go into `ErrorBanner`. A successful create clears the form.
- **Data flow:** each page fetches with `useState`/`useEffect` and re-fetches after every create, edit or delete instead of patching local state. If loading fails, the page shows `ErrorBanner` instead of the content.
- **Fixed wording:** empty list "No projects yet. Create your first project."; not found "Project not found." (rendered through `NotFoundPage`).
- **Delete:** `window.confirm("Delete project \"X\" and its N tasks?")`, where N is `taskCount` from the latest fetch. OK deletes the project and navigates to `/projects`; Cancel changes nothing.
- Plain CSS in `src/index.css`, native interactive elements only.

## Cross-Story Dependencies

- Story 2.1 creates the database, `ServiceResult`, the validation and projection patterns, the form-error pattern and the test helpers. Story 2.2 reuses them unchanged: same `ValidateAndApply`, same `ToDto`, same form component.
- Builds on Epic 1: extend the existing `client.ts` (`ApiError`, normalized field keys), `ErrorBanner`, `NotFoundPage`, routing and `TaskFlowApiFactory`; don't replace them.
- Epic 3 (Story 3.1) adds `TaskItem`, `Project.Tasks`, the required FK with `OnDelete(Cascade)` and a real `TaskCount = p.Tasks.Count()`. Keep `ProjectService.DeleteAsync` removing only the project row (don't load tasks), so the database cascade does the work later. The project page gains the task view in Story 3.2. `TimeProvider`/`FixedTimeProvider` arrive in Story 3.1; they are not needed here.
- Planned for Day 4: 2.1 is 2.75 h and 2.2 is 1.25 h. Never cut validation or tests.
