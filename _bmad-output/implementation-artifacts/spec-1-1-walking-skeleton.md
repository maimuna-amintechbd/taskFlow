---
title: 'Story 1.1 — Walking skeleton: clone, run, see the API respond'
type: 'feature'
created: '2026-10-24'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'f20873371a3c9ea80be73e029c7c8ef955b02c90'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-1-context.md'
  - '{project-root}/CLAUDE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The repo has no application code. A reviewer can't run anything, and later stories have no shared patterns to build on.

**Approach:** Scaffold the backend, test and frontend projects from the standard templates. Wire one end-to-end call (`GET /api/health` through the Vite proxy), and set up the shared building blocks: `Program.cs` setup, the API client with error display, routing, and the test factory. Document the exact README commands and verify that each one works.

## Boundaries & Constraints

**Always:**
- Follow the architecture spine (AD-1, AD-9, AD-11, AD-12, AD-13) and CLAUDE.md.
- Pin every Microsoft package to 10.0.11. The only additions beyond the templates are Swashbuckle.AspNetCore.SwaggerUI 10.3.1, Microsoft.AspNetCore.Mvc.Testing 10.0.11 and react-router 8.5.x, and each is explained in the README or the implementation notes.
- The API runs on `http://localhost:5080`, HTTP only; Vite on 5173 with `strictPort`. The frontend calls only relative `/api/...` URLs, and only from `src/api/`.
- Explain unfamiliar C#/.NET concepts to the user in the final summary.
- Run every build and test command before claiming that it passes.

**Never:**
- No EF Core, DbContext, database or migrations (Story 2.1); no `TimeProvider` (Story 3.1).
- No CORS, `UseHttpsRedirection`, https profile, `react-router-dom`, UI kit, state library or frontend test framework.
- No repositories, interfaces for services, or base controllers.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Health OK | `GET /api/health` | 200 text `Healthy` | N/A |
| Dashboard, API up | open `/` | "API connected" | N/A |
| Dashboard, API down | proxy returns a 5xx with no JSON body, or fetch fails | `ErrorBanner`: "Cannot reach the server. Is the API running?" | `ApiError{status:0}` |
| Unexpected error | non-2xx JSON ProblemDetails | `ErrorBanner` shows the body's `title` | `ApiError{status, message:title}` |
| Field errors | 400 with `errors: {"$.Status": [...]}` | `fieldErrors.status` | keys normalized |
| Unknown route | `/does-not-exist` | `NotFoundPage` | N/A |

</frozen-after-approval>

## Code Map

- `README.md` -- currently just `# taskFlow`; replace it with prerequisites, commands and the architecture overview.
- `.gitignore` -- already covers `bin/`, `obj/`, `node_modules/`, `dist/` and `*.db*`; no change needed.
- `_bmad-output/planning-artifacts/architecture/architecture-taskFlow-2026-10-22/README-architecture-overview.md` -- copy its content (without its first note line) into the README.
- Greenfield: no existing app code to reuse or protect.

## Tasks & Acceptance

**Execution:**
- [x] repo root -- run the spine's scaffolding commands: `dotnet new sln -n TaskFlow -o backend`, `webapi --use-controllers`, `xunit`, `sln add`, test→API reference, `dotnet new tool-manifest -o .config` + `dotnet tool install dotnet-ef --version 10.0.11`, `npm create vite@9.3.1 frontend -- --template react-ts` -- matches the Structural Seed.
- [x] `backend/TaskFlow.Api/TaskFlow.Api.csproj` -- OpenApi 10.0.11 (template) + SwaggerUI 10.3.1 -- NFR-6.
- [x] `backend/TaskFlow.Api/Program.cs` -- controllers + `JsonStringEnumConverter(allowIntegerValues:false)`; `AddProblemDetails`/`UseExceptionHandler`; `AddHealthChecks` + `MapHealthChecks("/api/health")`; `MapOpenApi` + `UseSwaggerUI` → `/openapi/v1.json` in Development; no HTTPS redirection; remove the template WeatherForecast sample -- AD-2, AD-4 (output), AD-11.
- [x] `backend/TaskFlow.Api/Properties/launchSettings.json` -- only the `http` profile, `http://localhost:5080`, `launchBrowser:false` -- AD-11.
- [x] `backend/TaskFlow.Api/{Controllers,Services,Dtos,Models,Data}/` -- create empty folders with `.gitkeep` so the layout is visible -- Structural Seed.
- [x] `backend/TaskFlow.Api.Tests/TestInfrastructure/TaskFlowApiFactory.cs` -- `WebApplicationFactory<Program>` subclass (no DB yet); csproj + Mvc.Testing 10.0.11 -- AD-13.
- [x] `backend/TaskFlow.Api.Tests/HealthTests.cs` -- `Health_Get_ReturnsHealthy`; remove the template `UnitTest1.cs`.
- [x] `frontend/vite.config.ts` -- port 5173, `strictPort`, proxy `/api` → `http://localhost:5080` -- AD-11.
- [x] `frontend/src/api/client.ts` -- `request<T>()`, `ApiError`, JSON-only parsing, key normalization, "Cannot reach the server" mapping -- AD-12.
- [x] `frontend/src/api/health.ts` -- `getHealth()` via `request<string>`; the client returns text for a non-JSON 2xx.
- [x] `frontend/src/main.tsx`, `App.tsx` -- `BrowserRouter` from `react-router`, a layout with `<nav>` links (Dashboard, Projects, Tasks), routes `/`, `/projects`, `/tasks`, `*`.
- [x] `frontend/src/pages/{DashboardPage,ProjectsPage,TasksPage,NotFoundPage}.tsx` -- the Dashboard shows "API connected" or the `ErrorBanner`; the others are placeholders.
- [x] `frontend/src/components/ErrorBanner.tsx` -- `role="alert"` message box.
- [x] `frontend/src/index.css` -- minimal plain CSS; remove the template's demo assets and CSS.
- [x] `README.md` -- prerequisites, the run/test/build commands from the spine (without the migrations line), and the architecture overview.

**Acceptance Criteria:**
- Given a clone with the .NET 10 SDK and Node 22.22+, when I run `cd backend && dotnet run --project TaskFlow.Api`, then the API listens on 5080 and `/swagger` loads Swagger UI.
- Given both apps running, when I open `http://localhost:5173`, then the nav is keyboard-reachable and the Dashboard shows "API connected".
- Given the repo, when I list the added packages, then only the three allowed additions are present and `react-router-dom` is absent.
- Given `backend/`, when I run `dotnet test`, then `Health_Get_ReturnsHealthy` passes; `npm run build` in `frontend/` passes.

## Implementation Notes

- Dependencies added (also in README): Swashbuckle.AspNetCore.SwaggerUI 10.3.1 (browse page for the built-in `/openapi/v1.json`), Microsoft.AspNetCore.Mvc.Testing 10.0.11 (`WebApplicationFactory`), react-router `~8.5.0` (resolved 8.5.0). `react-router-dom` is absent from `package-lock.json`.
- .NET 10 generates `public partial class Program` for top-level statements, so `WebApplicationFactory<Program>` works without adding one by hand.
- Removed `UseAuthorization()` from the template pipeline: there is no auth in scope, so it did nothing.
- `TaskFlow.Api.http` kept from the template but pointed at `http://localhost:5080/api/health` (the WeatherForecast sample is gone).
- `client.ts` falls back to `res.statusText`, then "Request failed with status N.", when a non-2xx JSON body has no `title`; duplicate keys after normalization are merged.
- The health endpoint is minimal-API health checks, so it does not appear in the OpenAPI document (`paths` is empty until controllers exist). Epic 5's "Swagger lists every endpoint" check should account for this.
- Browser manual check could not be done: the Claude in Chrome extension was unreachable. Client behaviour was instead exercised with Node against the live proxy, a fake ProblemDetails server, a closed port, and the proxy with the API stopped (502 text/plain → status 0).

- Browser check (2026-10-24): the Chrome extension was unreachable, so headless Chrome on the dev machine was used. Dashboard shows "API connected"; with the API stopped it shows the "Cannot reach the server. Is the API running?" banner (proxy 502 → status 0); `/does-not-exist` renders NotFoundPage; `/swagger` loads ("No operations defined" until Story 2.1). Nav items are native `<a href>` links with a `:focus-visible` outline; a live Tab-key walkthrough on the desktop is still worth doing once.
- Review fixes applied: README target-design note, subshell commands, mermaid `<br/>`; `OpenApiTests.cs` (2 tests); removed template `frontend/README.md` and favicon. Verification after fixes: `dotnet build` 0 warnings/0 errors, `dotnet test` 3 passed, `npm run build` ok.

## Spec Change Log

## Review Triage Log

| # | Layer | Finding | Verdict | Evidence / route |
|---|---|---|---|---|
| B1 | blind | README overview describes unbuilt features (DB, tasks, rules, SQLite tests) | medium | Copied verbatim per spec; code has only health. **patch**: add "target design" note. |
| B2 | blind | README claims URL-filter enum rejection; converter only covers JSON | medium | Same root as B1 (target-design text); AD-4 assigns query parsing to Story 3.2/4.1. Grouped with B1 → **patch**. |
| B3 / E12 | blind, edge | Test/build block fails when pasted as one sequence | medium | `cd frontend` runs from `backend/`. **patch**: subshells. |
| B4 | blind | Mermaid `\n` may render literally on GitHub | low | Direct correction to `<br/>`. **patch**. |
| B5 / E1 / V-o2 | all | `res.json()` throws SyntaxError on empty/malformed JSON body | low | API never sends JSON-typed empty bodies (ProblemDetails or 204 without content-type); fix adds a guard → rejected (unlikely + adds complexity). |
| E2 | edge | Connection drop during body read → TypeError | low | Rare on localhost; adds guard → rejected. |
| B6 / E3 / V-o1 | all | Any non-JSON 5xx shown as "Cannot reach the server" | low | Spec/AD-12 mandate this mapping; exceptions go through UseExceptionHandler → JSON; no health checks registered so no 503 text. Rejected; revisit if health checks are added. |
| B7 / E4 | blind, edge | AbortError mapped to unreachable | low | No caller passes a signal (pages use an ignore flag). Rejected. |
| E5 | edge | No fetch timeout; hung API leaves "Checking…" | low | Local dev only; adds behavior. Rejected. |
| B8 / E9 | blind, edge | Non-JSON 2xx returned as text typed `T` (e.g. 204) | low | DELETE callers will use `request<void>`; `''` is harmless. Rejected. |
| B9 / E6 | blind, edge | Keys like `request` / `$` match no input | false | AD-12 routes unmatched keys to `ErrorBanner` at form level (Story 2.1); client correctly passes them through. |
| E7 | edge | Nested keys only first segment lowercased | false | AD-9 DTOs are flat; no nested keys can occur. |
| E8 | edge | Object error value renders `[object Object]` | false | ASP.NET ValidationProblemDetails values are always `string[]`. |
| B10 | blind | Program.cs behaviors beyond health untested | medium | OpenAPI/Swagger part grouped with V1 → **patch**; ProblemDetails/enum converter have no observable endpoint yet (verification layer skipped them too) → rejected for now. |
| V1 | verify | Swagger UI / OpenAPI document untested | medium | Pre-verified gap. **patch**: `OpenApiTests.cs`. |
| V2 | verify | Client error mapping has no repeatable verification | medium | Pre-verified; spec forbids FE test framework → **defer** (deferred-work.md). |
| E11 | edge | Enum converter only on MVC JsonOptions; OpenAPI schema may show ints | maybe-false | Cannot observe until enums exist in DTOs (Story 3.1); would be medium → **defer** unverified. |
| E10 | edge | `/api` proxy prefix also matches `/apiary` | false | No frontend route starts with `/api` (routes: `/`, `/projects`, `/tasks`). |
| B11 | blind | Spec `in-review` vs sprint `in-progress`; browser check pending | low | Step 5 syncs sprint status; browser check is being done with the user before `done`. Rejected (process, no code fix). |
| B12 | blind | No `global.json` / `engines` | low | README states prerequisites; pinning SDK can break newer SDKs. Rejected. |
| B13 | blind | `package-lock.json` missing from diff | false | Excluded from the review diff on purpose; file exists, contains no `react-router-dom`, will be committed. |
| B14 | blind | Template leftovers: frontend README, Vite favicon | low | Direct deletions. **patch**. |
| E13 | edge | Claims file included in reviewed diff | low | Review-process note; no code fix. Rejected. |

## Design Notes

`request<T>()` contract (keeps every later page simple):

```ts
const res = await fetch(url, init).catch(() => { throw unreachable(); });
const isJson = res.headers.get('content-type')?.includes('json');
const body = isJson ? await res.json() : await res.text();
if (res.ok) return body as T;
if (!isJson && res.status >= 500) throw unreachable();     // Vite proxy: API down
throw new ApiError(res.status, body?.title ?? res.statusText, normalize(body?.errors));
```

## Verification

**Commands:**
- `cd backend && dotnet build` -- expected: 0 errors
- `cd backend && dotnet test` -- expected: 1 passed
- `cd frontend && npm run build` -- expected: success (tsc + vite build)
- `curl -s localhost:5080/api/health` while running -- expected: `Healthy`

**Manual checks:**
- Browser at `:5173`: nav works with Tab/Enter; Dashboard shows "API connected"; stop the API and reload → banner; `/does-not-exist` → NotFoundPage; `:5080/swagger` loads.
