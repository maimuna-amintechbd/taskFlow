# Review — Web/Reality Verification of ARCHITECTURE-SPINE.md

- **Reviewer lens:** Was every committed technical decision reality-checked (versions, technology fit, starter defaults) rather than asserted from training data?
- **Date:** 2026-10-22
- **Method:** I built and ran a throwaway backend (ASP.NET Core `webapi --use-controllers` + `xunit`, in a `.slnx`) and a throwaway frontend (`create-vite@9.3.1 --template react-ts` + `react-router@8`). The probes were written to match the spine's rules. Everything ran under `/tmp/claude-1000/-home-maimuna-projects-taskFlow/cbf6f7b5-eae2-41e4-baa5-b1dd5f42f9f6/scratchpad/verify/`. The repo was not touched, apart from adding this file.
- **Environment:** .NET SDK 10.0.111 with runtime 10.0.11 (Microsoft.AspNetCore.App / NETCore.App), Node 24.19.0, npm 11.17.0. NuGet and npm were both online.

## Verdict

**Mostly sound. Two items need a spine edit before stories are written.** All 12 claims were checked by running code, and most are confirmed as written. Problems found:

1. **AD-4 / Conventions conflict (real bug risk).** `[DisplayFormat(ConvertEmptyStringToNull = false)]` only works when `TaskListQuery` has settable or `init` **properties**. The Conventions table says DTOs, `TaskListQuery` included, are `record`s. If it is written as a *positional* record, the attribute cannot go on the parameter (compile error CS0592). With `[property: DisplayFormat(...)]` it compiles, but the binder ignores it and `?status=` silently binds as `null`, which means "All". That breaks the PRD rule that empty strings are rejected.
2. **AD-7 `CreatedAt` round-trip.** A UTC `DateTime` stored in SQLite comes back with `Kind=Unspecified` and is serialized **without `Z`** (`"2026-10-22T15:33:38.2424646"`). A browser that parses it will read it as local time.

Minor accuracy and wording items:

3. The automatic-400 error keys (AD-3/AD-9/AD-12) include a non-field key (`request`, or `""` for an empty body) next to `$.status`.
4. Query-string type errors use a PascalCase key (`ProjectId`).
5. `.config/dotnet-tools.json` is not where `dotnet new tool-manifest` writes the manifest in the .NET 10 SDK.
6. "react-router-dom no longer exists" is imprecise.
7. The scaffold's TypeScript is 6.0.3, and the decision log says 6.0.2.
8. xUnit v2 is the legacy line (xunit.v3 is at 4.0.2). Keeping the template default is defensible.

## Claim-by-claim results

### 1. `[DisplayFormat(ConvertEmptyStringToNull = false)]` on `[FromQuery]` class → `?status=` binds `""` — **CONFIRMED, with a trap**

Ran (GET via `WebApplicationFactory`):

| Shape | `?status=&priority=` (status has the attribute, priority does not) | no params |
| --- | --- | --- |
| `record TaskListQuery { [DisplayFormat(..)] public string? Status { get; init; } ... }` | `{"status":"''","priority":"<null>"}` | `status <null>` |
| `class TaskListQueryClass { [DisplayFormat(..)] public string? Status { get; set; } ... }` | `{"status":"''","priority":"<null>"}` | `status <null>` |
| positional `record TaskListQueryPositional([property: DisplayFormat(..)] string? Status, ...)` | `{"statusPropertyTarget":"<null>", ...}` — **attribute ignored** | `<null>` |
| positional record with `[DisplayFormat]` on the parameter | **does not compile**: `CS0592: Attribute 'DisplayFormat' is not valid on this declaration type` | — |

More results: `?status=%20` → `' '`, which is kept so the parser can reject it. `?status=7` → `'7'` arrives as a string, which is correct because the shared parser rejects it.

**Fix text (AD-4 bullet and Conventions "C# naming" row):**
> `TaskListQuery` is a `class` (or a `record` with `{ get; init; }` properties), **never a positional record**. MVC binds positional records through the constructor, so `[DisplayFormat]` is ignored there and `?status=` silently becomes `null` ("All"). Add a test: `GET /api/tasks?status=` → 400 with `errors.status`.

### 2. `JsonStringEnumConverter(allowIntegerValues: false)` output; `"status": 2` into `string?` → auto-400 — **CONFIRMED; the key set is more than `$.status`**

- Output: `{"status":"InProgress"}`, so names are sent. ✔
- `POST {"title":"x","status":2}` with the action parameter named `request` (non-nullable `SaveTaskRequest request`):
  ```json
  400 application/problem+json
  {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1","title":"One or more validation errors occurred.","status":400,
   "errors":{"request":["The request field is required."],
             "$.status":["The JSON value could not be converted to Api.Controllers.SaveTaskRequest. Path: $.status | LineNumber: 0 | BytePositionInLine: 11."]},
   "traceId":"00-…"}
  ```
- `"dueDate":"abc"` gives the same shape with `$.dueDate` plus `request`.
- Malformed JSON `{"title":` → `$.title` (the path where parsing stopped) plus `request`.
- Empty body → `errors: {"": ["A non-empty request body is required."], "request": ["The request field is required."]}`.
- `"projectId":"7"` (a string) is **accepted** as `7`, because the web defaults allow reading numbers from strings. Don't write a test that expects 400 for it.
- `{}` → 200 with all fields null, so the service validator runs as intended. ✔
- When the parameter was made nullable (`SaveTaskRequest? request`), `status:2` returned only `$.status`, **but an empty body returned 204/200 with `request == null`**. The service would then have to handle a null request.
- The `$.status` message leaks the CLR type name (`Api.Controllers.SaveTaskRequest`). That's harmless for the UI, which always sends strings, but it is ugly.

**Fix text (AD-12, append):**
> After normalization, any `fieldErrors` key that doesn't match a field on the form (for example `request`, or `""` for a missing body) goes into the `ErrorBanner`. An automatic 400 for a body type error always includes a `request` key next to `$.<field>`. Keep the controller parameter non-nullable (`SaveTaskRequest request`), so an empty body stays a 400.

### 3. `UseSetting("ConnectionStrings:TaskFlow", …)` overrides the lazy `AddDbContext` read, including the startup `MigrateAsync` — **CONFIRMED**

The probe endpoint returned (one factory):
```json
{"config":"Data Source=taskflow-test-5dfe…;Mode=Memory;Cache=Shared",
 "db":"Data Source=taskflow-test-5dfe…;Mode=Memory;Cache=Shared",
 "migratedWith":"Data Source=taskflow-test-5dfe…;Mode=Memory;Cache=Shared",
 "eagerAtStartup":"Data Source=taskflow-test-5dfe…;Mode=Memory;Cache=Shared"}
```
`migratedWith` was captured inside the `CreateScope()` block just before `await db.Database.MigrateAsync()`. The test log shows `Applying migration '…_Init'` against the in-memory DB, and no `taskflow.db` file was created by the tests. In .NET 10 even an *eager* `builder.Configuration.GetConnectionString` in `Program.cs` saw the override (`eagerAtStartup`). The lazy form is still the safer rule, so keep it.

### 4. Named shared-cache in-memory SQLite + one kept-open connection: FKs, cascade, migrations — **CONFIRMED**

```json
{"pragmaForeignKeys":[1],"before":2,"afterExecuteDelete":0,"afterTrackedDeleteTasksNotLoaded":0,
 "fkViolation":"SQLite Error 19: 'FOREIGN KEY constraint failed'."}
```
- EF Core opens its own connections, and each of them has `PRAGMA foreign_keys = 1`.
- `ON DELETE CASCADE` removes the child rows both for `ExecuteDeleteAsync` and for a tracked `Remove` of a project whose tasks were *not* loaded (the AD-6 pattern).
- The migration DDL contains `FOREIGN KEY … ON DELETE CASCADE`.
- `MigrateAsync` works, including EF 10's `__EFMigrationsLock` step.
- Isolation: a second factory created during a test saw only its own row.
- AC-4 file pattern: write through factory A, dispose it, `SqliteConnection.ClearAllPools()`, then read through factory B on the same file. Factory B saw A's rows, and afterwards the temp file could be deleted. ✔ Recommend `ClearAllPools()` before deleting the temp file. Without it, a pooled connection can keep the file open.

### 5. `WebApplicationFactory<Program>` without `public partial class Program` — **CONFIRMED**

The test project compiled and ran against top-level-statement `Program` with no partial declaration. In .NET 10 the source generator makes `Program` public.

### 6. EF Core 10 SQLite: `DateTimeOffset` ORDER BY unsupported; `DateOnly` stored as TEXT — **CONFIRMED (plus a Kind issue)**

- `OrderByDescending(t => t.CreatedAtDto)` → `NotSupportedException: SQLite does not support expressions of type 'DateTimeOffset' in ORDER BY clauses.`
- `DateTime` ordering works (`byDateTime: [2,1]`).
- Raw storage: `typeof(DueDate)` = `text`, value `2026-12-01`. `DateOnly` comparisons in `Where` translate correctly.
- **New finding:** after the round-trip, `CreatedAt.Kind` = `Unspecified`, and JSON gives `"2026-10-22T15:33:38.2424646"` (no `Z`).

**Fix text (AD-7, add a bullet):**
> SQLite returns `DateTime` values as `Kind=Unspecified`. In `OnModelCreating`, give `CreatedAt` a converter, `.HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc))`, so `createdAt` is serialized with a trailing `Z`. A test asserts that `createdAt` ends with `Z`.

(Alternative: never parse `createdAt` in the UI and display the server string as-is. The converter is simpler and correct.)

### 7. `dotnet test` from the folder that holds a `.slnx` (.NET 10 SDK) — **CONFIRMED**

`dotnet new sln` in SDK 10.0.111 creates `Verify.slnx` by default. Running `dotnet test` in that folder built both projects and ran every test (`Total tests: 8, Passed: 7, Failed: 1`). The one failure was a wrong assertion in my own probe; its output confirmed isolation. The later filtered runs passed.

### 8. Swashbuckle.AspNetCore.SwaggerUI 10.3.1 + Microsoft.AspNetCore.OpenApi 10.0.11, `/swagger` — **CONFIRMED**

- In tests: `/swagger` → `/swagger/index.html` 200, and `/swagger/index.js` contains `/openapi/v1.json`.
- Live `dotnet run`: `GET /swagger` → `301 → /swagger/index.html`.
- `/openapi/v1.json` → 200, `"openapi": "3.1.1"`, with the controller routes present.
- No NuGet warnings, and `dotnet list package --vulnerable --include-transitive` reported no vulnerable packages.
- 10.3.1 is the latest stable SwaggerUI.
- Note: `WebApplicationFactory` defaults to the Development environment, so Swagger is on in tests too. That is harmless.

### 9. Pin EF Core Sqlite/Design, Mvc.Testing (and dotnet-ef) to 10.0.11 — **CONFIRMED workable; rationale partly inaccurate**

- Restore, build, migrations (`dotnet-ef` 10.0.11 installed as a local tool) and tests all worked. There were no NU1605/NU1608 warnings and no known vulnerabilities.
- `dotnet list package --outdated` shows that 10.0.13 exists for every Microsoft package. The installed runtime (10.0.11) is two patches behind.
- Inaccuracy: EF Core is **not** part of the ASP.NET Core shared framework, so "framework/package patch skew" doesn't apply to it. Pinning everything to one patch is still a simple, consistent rule and fine for a beginner. Suggested wording: "pinned to 10.0.11 for one consistent patch across the repo. Bump all of them together (and the SDK/runtime) when a .NET patch ships."

### 10. Vite scaffold, React Router 8, Node, proxy and strictPort — **CONFIRMED (version detail differs)**

- `npm create vite@9.3.1 fe -- --template react-ts --no-interactive` exists and scaffolded successfully.
- Installed: `vite 8.3.5`, `@vitejs/plugin-react 6.1.2`, `react`/`react-dom 19.3.1`, `@types/react 19.3.0`, **`typescript 6.0.3`** (the template pins `~6.0.3`; the decision log says 6.0.2), `oxlint 1.89.0` (the template's linter, not ESLint), `@types/node 24.19.2`.
- `react-router@8.5.0`: `engines.node ">=22.22.0"` (the spine's ≥ 22.22 is correct). Peer dependencies: `react >=19.2.7`.
- Vite 8 engines: `^20.19.0 || >=22.12.0`.
- `react-router-dom` dist-tags: `latest` = **7.18.4**, with no v8. The package still exists, but installing it would silently give you v7.
- A tiny app using `BrowserRouter`, `Routes`, `Route`, `Link`, `useSearchParams`, `useParams` and `useNavigate` imported from `react-router` passed `npm run build` (`tsc -b && vite build`).
- Template `tsconfig.app.json` has `erasableSyntaxOnly: true` (a TS `enum` fails with TS1294) and `verbatimModuleSyntax: true`. `strict` is on by default in TS 6 (verified: implicit-any and null-to-string errors). The spine's "TS string unions, not enums" fits.
- Live: with `server: { port: 5173, strictPort: true, proxy: { '/api': 'http://localhost:5080' } }`:
  - `GET :5173/api/health` → `200 text/plain` (proxied).
  - `GET :5173/api/verify/missing` → proxied ProblemDetails.
  - Deep link `:5173/tasks?status=Todo` → `200 text/html` (SPA fallback).
  - A second `vite` on the same port exited 1 with `Error: Port 5173 is already in use`.

**Fix text (Stack table, react-router row):**
> Import from `react-router`. The `react-router-dom` package stops at v7 (latest 7.18.4), so never install it, because it would pull v7.

**Fix text (Stack table, TypeScript row):** `~6.0.3 (template default; strict on, erasableSyntaxOnly ⇒ no TS enums)`.

### 11. `ValidationProblem(new ValidationProblemDetails(dict))` → 400 with `errors` as given; `NotFound()` → ProblemDetails — **CONFIRMED**

```text
400 application/problem+json {"title":"One or more validation errors occurred.","status":400,"errors":{"title":["Title is required."],"projectId":["Project 42 does not exist."]}}
404 application/problem+json {"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5","title":"Not Found","status":404,"traceId":"00-…"}
```
The keys are kept exactly as given (camelCase is not re-cased). Two notes:
- The service-built 400 has **no `type` or `traceId`**, unlike the automatic 400s. That's cosmetic.
- An **unknown route** (`/api/does-not-exist`) returns `404` with an **empty body**, not ProblemDetails, because there is no `UseStatusCodePages`. `client.ts` must handle a non-JSON or empty error body. The spine's "404 is ProblemDetails" holds for `NotFound()` only.

### 12. Other Stack and fit claims

| Claim | Result | Evidence |
| --- | --- | --- |
| `webapi` template ships Microsoft.AspNetCore.OpenApi 10.0.11, `http` + `https` profiles, no UI | CONFIRMED | Generated `Api.csproj` and `launchSettings.json` |
| xunit template: xunit 2.9.3 / runner 3.1.4 / Test.Sdk 17.14.1 / coverlet 6.0.4 | CONFIRMED | Generated `Api.Tests.csproj`. Advisory: these are the legacy v2 line (xunit.v3 latest 4.0.2, runner 4.0.1, Test.Sdk 18.11.0, coverlet 10.1.0). The template default is acceptable for the MVP. Note it as a known upgrade, not a hidden one. |
| dotnet-ef 10.0.11 as a local tool, `.config/dotnet-tools.json` | PARTLY WRONG (path) | In SDK 10.0.111, `dotnet new tool-manifest` writes **`./dotnet-tools.json`** (repo root), not `.config/`. `dotnet tool restore` still finds `.config/dotnet-tools.json`, and `dotnet new tool-manifest -o .config` creates it there. `dotnet ef migrations add` works with a `Program.cs` that calls `MigrateAsync` after `Build()` (EF stops the host at `Build()`). |
| `Data Source=taskflow.db` resolves in the API project folder | CONFIRMED for `cd backend && dotnet run --project TaskFlow.Api` | `dotnet run --project` runs with CWD = project dir, so the DB landed in `Api/`. Note that SQLite resolves the path against the **CWD**, not the content root, so running the built DLL from another folder puts the DB there. EF 10 also creates `-wal`/`-shm` files. The repo `.gitignore` already covers `*.db`, `*.db-shm` and `*.db-wal`. ✔ |
| `AddHealthChecks` + `MapHealthChecks("/api/health")` → `200 text/plain Healthy` | CONFIRMED | Test and live curl |
| `AddProblemDetails` + `UseExceptionHandler` | Builds and runs. The 500 path was not exercised | — |

**Fix text (Structural Seed / Stack, dotnet-ef row):**
> `.config/dotnet-tools.json`. Create it with `dotnet new tool-manifest -o .config`. (In the .NET 10 SDK, plain `dotnet new tool-manifest` writes `dotnet-tools.json` at the repo root instead. `dotnet tool restore` finds either.)

## What was not verified

- The 500 path through `UseExceptionHandler` (not exercised).
- Behavior on Windows/macOS (only Linux was run). The SQLite relative path and CWD behavior should be the same.
- React Router behavior in a real browser (only the build and a curl SPA fallback were checked).

## Reproduction (commands actually run)

```bash
dotnet new webapi --use-controllers -n Api -o Api; dotnet new xunit -n Api.Tests -o Api.Tests; dotnet new sln -n Verify
dotnet add Api package Microsoft.EntityFrameworkCore.Sqlite|Design --version 10.0.11; dotnet add Api package Swashbuckle.AspNetCore.SwaggerUI --version 10.3.1
dotnet add Api.Tests package Microsoft.AspNetCore.Mvc.Testing --version 10.0.11
dotnet new tool-manifest; dotnet tool install dotnet-ef --version 10.0.11; dotnet ef migrations add Init --project Api --output-dir Data/Migrations
dotnet test --logger "console;verbosity=detailed"            # from the folder holding Verify.slnx
dotnet list package --vulnerable --include-transitive; dotnet list package --outdated
npm create vite@9.3.1 fe -- --template react-ts --no-interactive; npm install; npm install react-router@8; npm run build
dotnet run --urls http://localhost:5080 & npx vite & curl :5173/api/health; npx vite  # second instance → port in use
```
