---
title: 'Story 2.1 — Create a project and see it in the list'
type: 'feature'
created: '2026-10-24'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'b9719c958cbf1b29875af7f77c6a016627eede81'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
  - '{project-root}/CLAUDE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** There is no database and no way to create or see projects, the containers every task will belong to.

**Approach:** Add the SQLite database (EF Core, migrations applied at startup), the `Project` entity, `POST /api/projects`, `GET /api/projects` and `GET /api/projects/{id}`, single-pass validation, and the Projects page with a create form and a list. This story sets the patterns Story 2.2 and Epic 3 reuse.

## Boundaries & Constraints

**Always:**
- Follow epic-2-context exactly: `ServiceResult`, `ValidateAndApply`, `ToDto`, the lazy DbContext registration, error messages, key names and test setup.
- Add only EF Core Sqlite and Design 10.0.11. All EF calls are async. Controllers have no `Async` suffix on action names.
- `TaskCount` is a constant `0` inside `ToDto` until Story 3.1.
- Explain new EF Core/C# concepts in the final summary.

**Never:**
- No edit/delete endpoints or project detail page (Story 2.2); no `TaskItem`, `TimeProvider` or cascade (Epic 3).
- No DataAnnotations validation attributes, FluentValidation, repositories, service interfaces, AutoMapper or EF InMemory provider.
- No HTML constraint attributes (`required`, `maxLength`) in forms.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Create, trimmed | `{name:"  Portfolio site  ", description:"  "}` | 201, `Location: /api/projects/{id}`, `{name:"Portfolio site", description:null, taskCount:0}` | N/A |
| Description trimmed | `description:"  My notes  "` | saved as `"My notes"` | N/A |
| Name missing/empty/whitespace | `{}`, `name:""`, `name:"   "` | 400 `errors.name` = "Name is required." | nothing saved |
| Name too long | 101 chars after trim | 400 "Name must be at most 100 characters." | nothing saved |
| Name at limit | 100 chars plus surrounding spaces | 201 | N/A |
| Description too long | 1001 chars | 400 `errors.description` = "Description must be at most 1000 characters." | nothing saved |
| Two errors | empty name and 1001-char description | 400 with both keys in one response | nothing saved |
| Duplicate name | the same name twice | both 201 | N/A |
| List order | "beta", "Alpha", "alpha" | 200 ordered by name (ordinal), then id | N/A |
| Get by id | existing / 9999 | 200 `ProjectDto` / 404 ProblemDetails | N/A |
| Empty body | no JSON body | 400 | automatic 400 |
| UI create error | submit an empty name | "Name is required." next to the Name input; typed values kept | other keys and the message go to `ErrorBanner` |
| UI empty list | no projects | "No projects yet. Create your first project." | load failure → `ErrorBanner` |

</frozen-after-approval>

## Code Map

- `backend/TaskFlow.Api/Program.cs` -- add `AddDbContext` (lazy config read), `AddScoped<ProjectService>()`, and `MigrateAsync` in a scope after `Build()`; keep everything else.
- `backend/TaskFlow.Api/appsettings.json` -- add `ConnectionStrings:TaskFlow` = `Data Source=taskflow.db`.
- `backend/TaskFlow.Api.Tests/TestInfrastructure/TaskFlowApiFactory.cs` -- currently empty; add the shared-cache in-memory DB (kept-open connection plus `UseSetting`; dispose the connection). `HealthTests`/`OpenApiTests` keep working unchanged.
- `frontend/src/api/client.ts` -- reuse `request<T>`, `ApiError` and `fieldErrors`; do not change.
- `frontend/src/components/ErrorBanner.tsx`, `pages/ProjectsPage.tsx` (placeholder, replace its body), `App.tsx` (routes unchanged).
- `README.md` -- "Run it locally" and "Test and build" sections.

## Tasks & Acceptance

**Execution:**
- [x] `backend/TaskFlow.Api/TaskFlow.Api.csproj` -- add Microsoft.EntityFrameworkCore.Sqlite and .Design 10.0.11 (Design `PrivateAssets=all`).
- [x] `backend/TaskFlow.Api/Models/Project.cs`, `Data/TaskFlowDbContext.cs` -- the entity and the `DbSet<Project> Projects`.
- [x] `backend/TaskFlow.Api/Data/Migrations/` -- generate with `dotnet tool restore && dotnet ef migrations add AddProjects --project backend/TaskFlow.Api --output-dir Data/Migrations`; commit the generated files and don't edit them by hand.
- [x] `backend/TaskFlow.Api/Services/ServiceResult.cs` -- `ResultKind`, `ServiceResult<T>` and non-generic `ServiceResult`, with factories.
- [x] `backend/TaskFlow.Api/Dtos/ProjectDto.cs` (record), `Dtos/SaveProjectRequest.cs` (class, nullable `{get;set;}` properties).
- [x] `backend/TaskFlow.Api/Services/ProjectService.cs` -- `ListAsync`, `GetByIdAsync`, `CreateAsync`, `ValidateAndApply`, `ToDto`; list ordered by `Name`, then `Id`.
- [x] `backend/TaskFlow.Api/Controllers/ProjectsController.cs` -- `List`, `GetById`, `Create` with an inline `switch`; `Create` returns `CreatedAtAction(nameof(GetById), …)`.
- [x] `Program.cs`, `appsettings.json`, `TaskFlowApiFactory.cs` -- as in the Code Map.
- [x] `backend/TaskFlow.Api.Tests/TestInfrastructure/ApiJson.cs`, `HttpClientExtensions.cs` -- `ApiJson.Options`; `CreateProjectAsync(name, description)`.
- [x] `backend/TaskFlow.Api.Tests/Projects/CreateProjectTests.cs`, `ListAndGetProjectTests.cs` -- one test per matrix API row, plus "invalid saves nothing" (list stays empty).
- [x] `frontend/src/api/types.ts`, `api/projects.ts` -- `ProjectDto`/`SaveProjectRequest` (`T | null`); `listProjects`, `createProject`.
- [x] `frontend/src/components/FieldError.tsx`, `EmptyState.tsx`, `ProjectForm.tsx` -- presentational; `ProjectForm` takes `onSubmit` and `fieldErrors` props and converts `''` to `null`.
- [x] `frontend/src/pages/ProjectsPage.tsx` -- list (name plus "N tasks"), empty state, create form; clears and re-fetches on success; unmatched keys and the message go to `ErrorBanner`.
- [x] `frontend/src/index.css` -- small styles for the form, field errors and the list.
- [x] `README.md` -- note that the first run creates `backend/TaskFlow.Api/taskflow.db`; add a developer-only migrations command block.

**Acceptance Criteria:**
- Given no `taskflow.db`, when I start the API, then it creates the file with a `Projects` table, with no manual step.
- Given I create a project and restart the API, when I list projects, then it is still there.
- Given `ProjectsController`, when I read it, then it contains no DbContext and no rules; `ProjectService` owns every EF query.

## Implementation Notes

- Review fixes: ordinal-order test now creates "alpha" before "Alpha"; description 1000-char boundary test; OpenAPI attributes for GetById 200 and ValidationProblemDetails 400 plus `OpenApi_GetProjectById_Documents200Response`; README test/reset/path wording; CSS button scoping and long-name wrapping; `PROJECT_FORM_FIELDS` moved to `components/projectFormFields.ts` (keeps ProjectForm.tsx component-only, lint clean).
- Verification (2026-10-24): `dotnet build` 0 warnings/0 errors; `dotnet test` 20/20; `npm run build` ok; `npm run lint` clean. Fresh start created `backend/TaskFlow.Api/taskflow.db`; POST "  Demo  " → 201 `Location /api/projects/1`, name "Demo"; after API restart the project was still listed. Headless Chrome (DevTools protocol): empty state shown; empty-name submit → "Name is required." directly after the Name input, typed description kept; valid create → listed as "Portfolio site · 0 tasks", form cleared.

## Spec Change Log

## Review Triage Log

| # | Layer | Finding | Verdict | Evidence / route |
|---|---|---|---|---|
| VG1 | verify | Ordering test can't tell ordinal from case-insensitive (Alpha created before alpha) | medium | Pre-verified. **patch**: create "alpha" first. |
| VG2 / B5 | verify, blind | Description 1000-char boundary + trim-before-check untested; omitted description → null not asserted | medium | Pre-verified; name has the boundary test, description does not. **patch**. |
| VG3 | verify | Persistence across restart only manual | low | Already scheduled as the AC-4 temp-file test in Story 3.3; checked manually here (restart kept "Demo"). Rejected for this story. |
| VG4 | verify | ProjectsPage behaviour has no automated tests | medium | No FE test runner by architecture decision → **defer**. |
| VG-o / B3 | verify, blind | README says fresh DB "per test class"; actually per test | low | xUnit creates a class instance per test; direct doc fix → **patch**. |
| B1 / B2 | blind | OpenAPI: `GET /api/projects/{id}` has no 200; POST 400 typed as ProblemDetails | medium | Verified by fetching `/openapi/v1.json` (only `404` listed; 400 → `ProblemDetails`). **patch** + OpenAPI test. |
| B4 | blind | Reset instruction ignores WAL files / running API | low | `-wal`/`-shm` seen beside the DB while running. Direct doc fix → **patch**. |
| E1 / E10 / B12 | edge, blind | DB path relative to working directory | low | README command puts it in `backend/TaskFlow.Api/` (verified); other start methods could create a second DB. AD-10 accepted design → README note **patch**. |
| E9 | edge | README wrong: DB lands in `backend/` | false | Verified: `dotnet run --project TaskFlow.Api` from `backend/` created `backend/TaskFlow.Api/taskflow.db`, nothing in `backend/`. |
| B7 | blind | Global `button` style makes every future button primary-green | low | Story 2.2 adds Delete; direct CSS scoping → **patch**. |
| B8 | blind | 100-char unbroken names overflow list row | low | Allowed by rules; direct CSS fix → **patch**. |
| B9 | blind | `FORM_FIELDS` duplicates ProjectForm's fields | low | Story 2.2 edit page would copy it; trivial export → **patch**. |
| B10 / E7 | blind, edge | Duplicate list loader; no guard on reload; theoretical race | low | Local single user; React ignores late setState; fix adds structure. Rejected. |
| B6 | blind | Raw framework text for `{"name":5}` / empty-body keys | low | UI never sends non-strings; empty body still 400. Rejected. |
| B11 | blind | No focus move / aria-live after submit | low | Basic keyboard access met (NFR-8); polish belongs to Story 5.2. Rejected. |
| E2 | edge | Missing connection string → unclear startup error | low | Key always present in appsettings; adds guard. Rejected. |
| E3 | edge | Row deleted between save and re-read → NRE | low | Single user, no concurrent delete path in this story. Rejected. |
| E4 | edge | Emoji counted as 2 UTF-16 units | low | Unrealistic for project names; adds complexity. Rejected. |
| E5 | edge | Zero-width-only name accepted | low | Unlikely input; adds Unicode guard. Rejected. |
| E6 | edge | `/api/projects/abc` → empty-body 404 | low | UI never builds such URLs; client maps it to "Not Found". Rejected. |
| E8 | edge | Pooled connections keep test DBs alive | maybe-false | Would only be low (test memory); 22 tests run in ~2 s. Rejected. |

## Verification

**Commands:**
- `cd backend && dotnet build` -- expected: 0 errors, 0 warnings
- `cd backend && dotnet test` -- expected: all pass (3 existing + the new project tests)
- `cd frontend && npm run build` -- expected: success
- `curl -s -X POST localhost:5080/api/projects -H 'Content-Type: application/json' -d '{"name":"  Demo  "}'` -- expected: 201 with `"name":"Demo"`

**Manual checks:**
- Browser `:5173/projects`: empty state; submit an empty name → message by the input; create → appears in the list; restart the API → still listed. (The Chrome extension may be unavailable; headless Chrome on the dev machine is acceptable.)
