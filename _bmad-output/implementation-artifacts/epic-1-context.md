# Epic 1 Context: Run TaskFlow locally (walking skeleton)

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Produce a walking skeleton of TaskFlow that a portfolio reviewer can clone and run: the README commands start the ASP.NET Core API and the React/Vite frontend, the frontend reaches the API through the Vite proxy, Swagger UI loads, and `dotnet test` passes. No features exist yet. The real value is the set of patterns this epic puts in place (folder layout, `Program.cs` setup, port/proxy topology, the frontend API client and error display, routing, and the test factory), because every later story reuses them. It also makes the first pass at the "runs locally from documented commands" release check and plumbs the general-error part of useful error messages.

## Stories

- Story 1.1: Walking skeleton — clone, run, see the API respond

## Requirements & Constraints

- **Local run:** the README lists prerequisites (.NET 10 SDK, Node.js 22.22+, 24 LTS recommended) and the exact run, test and build commands. Every command in it must actually be run and work on this machine. A fresh-clone run must eventually take ≤ 10 minutes.
- **API docs:** an OpenAPI/Swagger page in Development only.
- **Errors:** if the backend can't be reached, or it returns an unexpected error, the UI shows a general message, never a blank screen.
- **Cost:** only free, open-source tools. No paid or cloud services.
- **Code clarity:** readable for a beginner. HTTP, business rules and data access are kept apart, with no needless layers.
- **Usability:** nav and actions work with the keyboard. Use native interactive elements only.
- **Definition of Done:** acceptance criteria met; `dotnet build`, `dotnet test` and `npm run build` pass; behavior checked in the browser; committed with a clear message. Never claim a command passed unless it was actually run.
- **Scope:** single-user, with no auth, CORS, HTTPS, CI, containers or deployment.

## Technical Decisions

- **Scaffolding (from the repo root):** `dotnet new sln -n TaskFlow -o backend` (→ `backend/TaskFlow.slnx`); `dotnet new webapi --use-controllers` → `backend/TaskFlow.Api`; `dotnet new xunit` → `backend/TaskFlow.Api.Tests`. Add both projects to the sln, and reference the API from the tests. `dotnet new tool-manifest -o .config` plus `dotnet-ef` 10.0.11 as a local tool. `npm create vite@9.3.1 frontend -- --template react-ts`.
- **Allowed packages beyond the templates (this epic):** Swashbuckle.AspNetCore.SwaggerUI 10.3.1 (UI only; .NET 10 generates the OpenAPI document but has no browse page), Microsoft.AspNetCore.Mvc.Testing 10.0.11, and react-router 8.5.x. All Microsoft packages are pinned to 10.0.11. **Never install `react-router-dom`.** EF Core Sqlite/Design come in Epic 2. Every new dependency must be explained, with a reason recorded.
- **Backend layout:** `Controllers/` (HTTP only, no rules, no DbContext), `Services/` (all rules and EF queries; concrete classes, `AddScoped`, no interfaces), `Dtos/`, `Models/`, `Data/`. No repositories, unit of work, AutoMapper or base controllers. Controller action names have no `Async` suffix.
- **`Program.cs`:** `AddControllers().AddJsonOptions(...)` with `JsonStringEnumConverter(allowIntegerValues: false)`; `AddProblemDetails()` + `UseExceptionHandler()` so unexpected failures become 500 ProblemDetails; health checks mapped at `GET /api/health` → 200 text `Healthy`; OpenAPI at `/openapi/v1.json` with Swagger UI at `/swagger`, Development only.
- **Topology:** the API is on `http://localhost:5080`. `launchSettings.json` keeps only the `http` profile, with `launchBrowser: false`. Remove `UseHttpsRedirection` and add no CORS policy. Vite runs on 5173 with `strictPort: true` and proxies `/api` → `http://localhost:5080`. The frontend calls only relative `/api/...` URLs.
- **Frontend API client (`src/api/client.ts` `request<T>()`):** the only place that does HTTP. It parses a body only when the `Content-Type` contains `json`. A non-2xx response throws `ApiError { status, message, fieldErrors }`, where `message` is the body's `title`. A fetch failure, or a 5xx with no JSON body (what the Vite proxy returns when the API is down), gives `status: 0` and the message "Cannot reach the server. Is the API running?". Field-error keys are normalized once: a leading `$.` is dropped and the first letter lowercased.
- **Frontend structure:** `src/api/`, `src/pages/*Page.tsx`, `src/components/*.tsx`. Only pages import `src/api/`; components take props and callbacks. Each page fetches its own data with `useState`/`useEffect`. No global store and no data-fetching library. Routing is a declarative `BrowserRouter` imported from `react-router`. Plain CSS in `src/index.css`, no UI kit.
- **Tests:** `TaskFlow.Api.Tests/TestInfrastructure/TaskFlowApiFactory : WebApplicationFactory<Program>`. Tests call the real pipeline through `HttpClient`, and test names follow `Method_Scenario_Expected` (e.g. `Health_Get_ReturnsHealthy`). Never use the EF InMemory provider. There are no automated frontend tests in the MVP.
- **Repo shape:** match the structural seed: `backend/TaskFlow.slnx`, `backend/TaskFlow.Api`, `backend/TaskFlow.Api.Tests`, `frontend/`, `.config/dotnet-tools.json`, root `README.md`. The README also includes the architecture overview section, copied as-is from the companion `README-architecture-overview.md` in the architecture folder.

## UX & Interaction Patterns

- A layout with keyboard-reachable nav links: Dashboard (`/`), Projects (`/projects`), Tasks (`/tasks`).
- The Dashboard page calls `/api/health` and shows "API connected". On failure it shows an `ErrorBanner` with the `ApiError` message, never a blank screen.
- `/projects` and `/tasks` show placeholder headings. Unknown URLs (`*`) render `NotFoundPage`.
- `ErrorBanner` and `NotFoundPage` are the shared building blocks later pages reuse for general errors and 404s.

## Cross-Story Dependencies

- Story 1.1 is the foundation for every later epic. The client, `ErrorBanner`, the routing, `Program.cs` and the test factory are extended, not replaced.
- Deferred to Story 2.1: the database (EF Core Sqlite/Design packages, `TaskFlowDbContext`, migrate-on-start, `taskflow.db`), the shared-cache in-memory SQLite setup in the factory, `ApiJson`/`CreateProjectAsync` helpers, `ServiceResult`, and the README's `dotnet ef migrations add` line and "creates taskflow.db" note. Don't add these in Epic 1.
- Deferred to Story 3.1: `TimeProvider` registration and `FixedTimeProvider`/`factory.Clock`.
- Epic 5 re-verifies the README on a fresh clone and checks Swagger lists every endpoint, so keep the README commands exact.
- Scheduled for Day 3 (2.5 h plus a 1.5 h buffer for setup problems). Unused buffer is the only time allowed for the later UI-polish story.
