# Review — Reconciliation + Good-Spine Checklist

- **Spine:** `ARCHITECTURE-SPINE.md` (draft, 2026-10-22), with `.memlog.md`
- **Inputs walked:** `prd.md` (FR-1…FR-11, NFR-1…NFR-9, AC-1…AC-7, §6, §8 risks), `addendum.md`, `CLAUDE.md`
- **Reviewer:** independent (rubric + reconcile lens). The spine was not edited.
- **Date:** 2026-10-22

## Verdict

**Approve with changes.** The spine is strong. It covers every CLAUDE.md convention, every addendum note and every AC, and its rules are concrete enough to enforce in review or with a grep. Fix one **high** finding before Story 1.1: an empty due date from the form cannot reach the "clear it" rule. Fix the **medium** findings before the first frontend story. They are all quiet requirements or wire-level edge cases where two stories would otherwise make different choices.

| Severity | Count |
| --- | --- |
| Critical | 0 |
| High | 1 |
| Medium | 5 (M2–M6) |
| Low | 8 |

---

## Part 1 — Reconciliation

Legend: ✅ covered · ⚠️ partly covered or at risk · ❌ dropped

### FR-1 Create a project

| Bullet | Status | Where / note |
| --- | --- | --- |
| Name required, 1–100 after trim | ✅ | AD-3 ("Length limits are checked after trimming"), AD-8 trim. The limit values stay in the PRD, which is fine. |
| Description optional ≤ 1,000 | ✅ | AD-3, AD-8 |
| Invalid → nothing saved, an error per failing field | ✅ | AD-3 single pass, AD-2 `Invalid` |
| Names need not be unique | ✅ (implicit) | No unique index is in the ER diagram. Low risk. |
| Saved trimmed | ✅ | AD-8 |

### FR-2 View projects

| Bullet | Status | Where / note |
| --- | --- | --- |
| List shows name + task count | ✅ | AD-9 `ProjectDto.taskCount` |
| Empty-state message with no projects | ❌ | Not mentioned anywhere. See **M4**. |
| Project task view: name, description, tasks, same columns and order as FR-6 | ✅ | `ProjectPage` + shared `TaskTable` + AD-7 ordering |
| Empty-state message with no tasks | ❌ | See **M4** |
| Unknown project → 404 **and the UI says so** | ⚠️ | The API side is covered (AD-2). The UI side is undecided: `NotFoundPage` exists only for the `*` route, and AD-12 sends "any other error" to `ErrorBanner`. See **M3**. |

### FR-3 Edit a project

| Bullet | Status | Where / note |
| --- | --- | --- |
| Same rules as FR-1, persists | ✅ | AD-3, AD-8, AD-10 |
| Unknown → 404 | ✅ | AD-2. Whether 404 or 400 wins on an invalid body is undecided (**L2**). |

### FR-4 Delete a project and its tasks

| Bullet | Status | Where / note |
| --- | --- | --- |
| Confirmation **states the task count** | ✅ | AD-12 ("states the project's `taskCount`") |
| Project and tasks gone; dashboard excludes them | ✅ | AD-6, FR-10 map |
| Database enforces the cascade | ✅ | AD-6. See **L5** for making the test prove it. |

### FR-5 Create a task

| Bullet | Status | Where / note |
| --- | --- | --- |
| Title 1–200 after trim; missing, empty or whitespace rejected; saved trimmed | ✅ | AD-3, AD-8 |
| Project required and existing; unknown → validation error | ✅ | AD-3 (DB check in the same pass), AD-9 key `projectId`. An empty `projectId` from the `<select>` is at risk: see **H1**. |
| Defaults `Todo` / `Medium` | ✅ | AD-8 |
| Enum names only, any case; numbers, misspellings and empty strings rejected, in body and filters | ✅ | AD-4. A numeric JSON value (`"status": 2`) produces a framework message and a `$.status` key: see **M5**. |
| Due date optional; ≥ today | ✅ | AD-7 |
| Description ≤ 2,000 | ✅ | AD-3 |
| Returned with id | ✅ | AD-9 `201 TaskDto` |

### FR-6 View tasks

| Bullet | Status | Where / note |
| --- | --- | --- |
| Shows title, project, status, priority, due date | ✅ | AD-9 `TaskDto` (incl. `projectName`) |
| **Newest first** by creation time | ✅ | AD-7 (`CreatedAt` desc, `Id` desc as tie-break, which matters with `FixedTimeProvider`) |
| Unknown → 404 | ✅ | AD-2. The UI side for `/tasks/:id/edit` belongs to **M3**. |

### FR-7 Edit a task

| Bullet | Status | Where / note |
| --- | --- | --- |
| Whole task sent; title, project, status and priority required; no defaults | ✅ | AD-8 |
| Absent **or empty** description or due date is cleared | ⚠️ | This holds for description. For due date it cannot work as specified: see **H1**. |
| **Unchanged past due date accepted**, new past date rejected, clearing allowed | ⚠️ | The backend side is covered (AD-7). The frontend can still block it with `<input type="date" min=…>`: see **M5**. |
| Persists after restart | ✅ | AD-10, AD-13 (AC-4 file test) |
| Unknown → 404 | ✅ | AD-2 |

### FR-8 Delete a task

| Bullet | Status | Where / note |
| --- | --- | --- |
| Confirmation | ✅ | AD-12 `window.confirm` |
| Gone; dashboard updates | ✅ | FR-10 map |
| Unknown → 404 | ✅ | AD-2 |

### FR-9 Filters

| Bullet | Status | Where / note |
| --- | --- | --- |
| Status, priority or both (AND) | ✅ (implicit) | `TaskService.ListAsync`. AND is the natural reading. Low risk. |
| "All" option | ✅ | AD-12 (omitted parameter = All) |
| Same filters in the project task view | ⚠️ | The capability map says yes. The frontend route table lists only `GET /api/tasks?projectId=` for `ProjectPage`. See **L1**. |
| Empty-state when nothing matches | ❌ | See **M4** |
| Invalid status or priority → 400 | ✅ | AD-4 |
| **Empty strings rejected** | ⚠️ | `?status=` and `?priority=` are covered (AD-4 `DisplayFormat`). `?projectId=` binds to `null`, which means "All". See **M6**. |
| **Unknown projectId → 400** | ✅ | AD-9 |
| (wire) `TaskListQuery` actually binds from the query | ⚠️ | See **M6**: `[ApiController]` infers `[FromBody]` for complex types. |

### FR-10 Dashboard

| Bullet | Status | Where / note |
| --- | --- | --- |
| Total / completed / pending definitions | ✅ | AD-9 `DashboardDto`, `DashboardService` |
| **Recalculated each time it is opened**, not live | ✅ | Map: "counts from the DB on each request". AD-12: each page fetches in `useEffect`, and there is no global store or cache. |
| Zero tasks → 0/0/0 | ✅ (implicit) | |

### FR-11 Errors

| Bullet | Status | Where / note |
| --- | --- | --- |
| Backend validates every write; 400 with a message per field naming the field and rule | ✅ | AD-3, AD-9, Conventions → Errors |
| Field message next to its input; **keeps what the user typed** | ✅ / ⚠️ | AD-12 covers it. Field keys that match no input (e.g. `request`) have no defined home: see **M5**. |
| Backend unreachable or unexpected error → general message, never blank | ⚠️ | AD-12 assumes a network failure gives `status: 0`. Behind the Vite proxy, a stopped API reaches the browser as an HTTP 5xx with a non-JSON body, so the `status: 0` branch never fires. See **M2**. |

### NFRs

| NFR | Status | Where / note |
| --- | --- | --- |
| NFR-1 Persistence | ✅ | AD-10 |
| NFR-2 Integrity | ✅ | AD-6 (required FK + cascade) |
| NFR-3 Local run, no manual SQL | ✅ | AD-10 `MigrateAsync`, README commands. The scaffold commands are missing: **L3**. |
| NFR-4 Free / OSS only | ✅ (implicit) | Every Stack entry is OSS. There is no cloud. |
| NFR-5 xUnit, real SQLite in-memory | ✅ | AD-13 |
| NFR-6 Swagger page | ✅ | AD-11, Stack (SwaggerUI) |
| NFR-7 < 1 s at 500 tasks | ✅ | Conventions (`AsNoTracking` + `Select`), Deferred indexes |
| NFR-8 Browsers, labels, keyboard | ✅ | Conventions → Styling & a11y (native controls, `<label htmlFor>`), `window.confirm` |
| NFR-9 Clarity, layers kept apart | ✅ | Paradigm, AD-1 |

### Release acceptance checks

| AC | Status | Where |
| --- | --- | --- |
| AC-1 valid title | ✅ | AD-3, AD-13 |
| AC-2 valid project | ✅ | AD-3, AD-6, AD-13 |
| AC-3 enums, body and filter | ✅ | AD-4, AD-13. The expected key for numeric JSON is unstated: **M5**. |
| AC-4 persists after restart | ✅ | AD-13 temp-file test plus the manual check |
| AC-5 cascade | ✅ | AD-6, AD-13 (**L5**) |
| AC-6 dashboard totals | ✅ | AD-13 |
| AC-7 documented commands | ✅ | README commands block |

### Addendum notes

| Note | Status | Where |
| --- | --- | --- |
| Stack fixed | ✅ | Stack |
| `TaskItem`, routes `/api/tasks` | ✅ | AD-5 |
| `JsonStringEnumConverter(allowIntegerValues:false)`, case-insensitive, numbers rejected, query needs the same check | ✅ | AD-4 |
| Required FK + cascade in EF config | ✅ | AD-6 |
| SQLite in-memory, one open connection per test | ✅ | AD-13 |
| DTOs, async throughout | ✅ | AD-9, Conventions → Async |
| Likely API surface | ✅ | AD-9 table (adds `GET /api/projects/{id}` and `/api/health`) |
| 400 ValidationProblemDetails, 404 for a route id, 400 for an unknown `projectId` in body or filter | ✅ | AD-2, AD-9 |
| PUT replaces the whole task | ✅ | AD-8 |
| Due-date-on-edit rule with `DateOnly` and `TimeProvider` | ✅ | AD-7 |
| Migrations preferred over `EnsureCreated` | ✅ | AD-10 |
| AC-4 via a temp SQLite file | ✅ | AD-13 |
| Settle the CORS origin and ports | ✅ | AD-11 (proxy, no CORS) |

### CLAUDE.md

| Rule | Status | Where |
| --- | --- | --- |
| Stack (.NET 10, EF Core, SQLite, React/TS/Vite, Swagger, xUnit) | ✅ | Stack |
| Separate frontend and backend directories | ✅ | Structural Seed |
| HTTP, business and data kept apart | ✅ | Paradigm, AD-1 |
| DTOs | ✅ | AD-9, Conventions |
| Async DB operations | ✅ | Conventions → Async |
| Avoid unnecessary abstractions | ✅ | AD-1 (no repository, UoW, AutoMapper or service interfaces). `ServiceResult` is justified by AD-2. |
| `TaskItem` naming | ✅ | AD-5 |
| Enums as strings, undefined rejected | ✅ | AD-4 |
| Cascade enforced in the EF relationship | ✅ | AD-6 |
| Tests on SQLite in-memory, not the InMemory provider | ✅ | AD-13 |
| Learning rule: no dependency without an explanation | ✅ | Stack "Why" column, Conventions → Dependencies |
| Learning rule: do not skip tests or validation silently | ✅ | Frontend tests are skipped *openly* (AD-13, Deferred). Backend validation is never cut. |
| Learning rules: explain concepts, one story at a time, run tests and builds, never claim a pass | n/a | Process rules. They belong in stories and the DoD, not in the spine. |
| Scope: single user, no auth, real-time or microservices | ✅ | Paradigm (one monolith), Deployment, PRD §6 |

---

## Part 2 — Findings

### H1 (high) — An empty due date from the form cannot be cleared. It turns into a framework 400.

The spine says:

> AD-3: "Every request-DTO property is nullable (`string?`, `int?`, `DateOnly?`)" … "`[ApiController]`'s automatic 400 is kept only for JSON the binder cannot read: malformed JSON or wrong types such as `"dueDate": "abc"`"
> AD-8: "A null or empty description or due date clears it."
> AD-12: "Dates stay `yyyy-MM-dd` strings from `<input type="date">` to the API."

An empty `<input type="date">` has the value `""`. If the frontend follows AD-12 literally, it sends `"dueDate": ""`. System.Text.Json cannot read `""` as a `DateOnly?`, so `[ApiController]` returns its automatic 400 with key `$.dueDate` and a message like "The JSON value could not be converted to System.Nullable`1[System.DateOnly]". This happens before the service runs, so:

- FR-7 ("a due date that is absent or **empty** is cleared") fails on the most common path, clearing the date in the form.
- FR-5 create with no due date fails too, unless the frontend happens to send `null`.
- The single-pass guarantee of AD-3 is lost: the user sees one cryptic error instead of every field error.

`projectId` has the same trap. A `<select>` with a "Choose a project" option sends `""`. JSON number handling in ASP.NET Core's Web defaults allows `"7"` but not `""`. The user gets a framework error under `$.projectId` instead of "Project is required."

One frontend story will send `""` and another will send `null`. Today the spine leaves this to chance.

**Fix (recommended, matches the AD-4 pattern of "strings in, parse in the service").** Replace the AD-3 DTO bullet and the auto-400 bullet with:

> - Every request-DTO property is nullable. `DueDate` is `string?` and is parsed in the service. Null, empty or whitespace means "no due date". Otherwise `DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)` must succeed, else add the error `dueDate: "Due date must be a valid date (yyyy-MM-dd)."`.
> - `[ApiController]`'s automatic 400 is kept only for JSON the binder cannot read: malformed JSON, or a JSON number/object where a string is expected (e.g. `"status": 2`).

Then add to AD-12:

> - The form sends `projectId` as a number, or `null` when nothing is chosen, never `""`. It sends `dueDate` as the input's string, and `""` is allowed and means "no due date".

(Alternative if `DateOnly?` must stay on the DTO: AD-12 says "the client sends `null`, never `""`, for an empty date or project", and AD-8 drops the word "empty" for due date. That is weaker, because a raw API caller still gets the framework message for `""`.)

### M2 (medium) — "Cannot reach the server" never fires behind the Vite proxy, and non-JSON error bodies are unhandled

> AD-12: "On a non-2xx response or a network failure, it throws `ApiError {…}`. A network failure gives `status: 0` and the message "Cannot reach the server.""

In the topology AD-11 chose, the browser always reaches Vite. When the API is stopped, Vite's proxy logs the error and answers the browser with an HTTP 5xx and an empty or plain-text body; current Vite sends 500 with no content. So:

- the `status: 0` branch never fires in development, which is the only environment there is; and
- if `client.ts` calls `response.json()` on every non-2xx, it throws a `SyntaxError` instead of an `ApiError`. That leads to an unhandled rejection or a blank page, which is the failure FR-11 bullet 3 forbids.

**Fix:** replace the first bullet of AD-12 with:

> - All HTTP goes through `src/api/client.ts` `request<T>()`. It throws `ApiError { status, message, fieldErrors }` for every failure and never lets a parse error escape. A `fetch` rejection gives `status: 0`. The response body is parsed only when `Content-Type` contains `json`. For a ProblemDetails body, `message = detail ?? title`. Otherwise (e.g. the Vite proxy's empty 500 when the API is down) the message is "Cannot reach the server or it failed. Is the API running?". Every page catches `ApiError` and renders `ErrorBanner`. No page renders blank on error.

Add to the Day-3 smoke test (or Story 1.x acceptance): stop the API, then load `/` and see the banner.

### M3 (medium) — "The UI says so" for 404 is undecided

FR-2 requires that a missing project gives 404 **and the UI says so**. The same need arises for `/tasks/:id/edit` with a deleted task. The spine has a `NotFoundPage` only for the `*` route. AD-12 sends "any other error" to `ErrorBanner`. With `NotFound()`, the ProblemDetails `title` is just "Not Found", and there is no `detail`. One page will show a banner saying "Not Found", another will redirect, and a third will render `NotFoundPage`.

**Fix:** add to AD-12:

> - When a page's initial `GET` of a route id (`/projects/:id`, `/tasks/:id/edit`) fails with `ApiError.status === 404`, the page renders `<NotFoundPage what="Project" />` ("Project not found." with a link back to the list) instead of its content. A 404 from a later PUT or DELETE (the item was deleted in another tab) shows `ErrorBanner` with "This project/task no longer exists."

### M4 (medium) — Empty-state messages were dropped

FR-2 has two empty states (no projects; a project with no tasks) and FR-9 has one (no task matches the filters). None of them appear in the spine, the components list or the capability map. These are quiet requirements that a story writer copying from the spine will miss. Without a shared component, the wording and placement will differ across three pages.

**Fix:** add `EmptyState` to the Structural Seed `components/` line, and add to AD-12:

> - Every list renders `<EmptyState>` when it has zero rows. Use "No projects yet. Create your first project." on `ProjectsPage`. Use "No tasks yet." when no filter is set. Use "No tasks match these filters." when a filter is set. `TaskTable` takes an `emptyMessage` prop, so `TasksPage` and `ProjectPage` share it.

Add to the Capability map: FR-2 row → "AD-12 (empty state)". FR-9 row → "AD-12 (empty state)".

### M5 (medium) — The frontend can pre-empt or swallow the backend's field errors

There are three related gaps. Together they threaten FR-11 bullet 2 ("messages next to the matching input") and FR-7's unchanged-past-date rule.

1. **HTML validation attributes are not decided.**
   - `required` makes the browser block submit with its own tooltip, so the backend's "Title is required." never shows. That diverges from AD-3's "one source of messages".
   - `min={today}` on the date input makes the browser **refuse to submit an unchanged past due date**, which FR-7 explicitly allows.
   - `maxLength` silently truncates typing.

   One form story will add these, because it feels like good practice, and another will not.
2. **Field keys that match no input.** When the body is unreadable, `[ApiController]`'s automatic 400 typically also adds an error under the parameter name (e.g. `request: ["The request field is required."]`), alongside `$.status` or `$.dueDate`. After normalization `request` matches no input. AD-12 does not say where such keys go, so they will be dropped silently.
3. **Test expectations for framework 400s.** AD-13 says tests "assert the keys in `errors`", but the expected key for `"status": 2` (`$.status`, not `status`) is not stated. AC-3 tests will be written two ways.

**Fix:** add to AD-12:

> - Forms use `<form noValidate>` and put no `required`, `min`, `max` or `maxLength` attributes on inputs. The backend is the only source of validation messages. (In particular, a `min` on the due-date input would block FR-7's "unchanged past date is accepted".)
> - A field error whose normalized key has no input on the current form is shown in `ErrorBanner`, never dropped.

Add to AD-13:

> - For bodies the binder rejects (e.g. `"status": 2`), tests assert 400 and that `errors` has a key `$.status`. For service-level errors, they assert the camelCase key (`status`).

### M6 (medium) — Wire-level gaps in the filter query: `[FromQuery]`, and `?projectId=`

1. **Binding source.** AD-4 and AD-9 say the list action takes a `TaskListQuery` class. Under `[ApiController]`, a complex-type parameter is inferred as `[FromBody]`, even on GET. Without an explicit `[FromQuery]`, the action reads a body that never arrives and returns 400 (or 415), and the filters never bind. This is the first thing Story "list tasks" will hit, and the spine should pin it.
2. **Empty `projectId`.** AD-4 handles `?status=` and `?priority=`. `ProjectId` is presumably `int?`. For `?projectId=`, MVC turns the empty string into `null`, which means "All". So an empty filter is silently treated as "no filter". FR-9 says invalid filter values are "rejected with a validation error, not silently ignored", and FR-5 groups empty strings with invalid values.

**Fix:** add to AD-4:

> - The list action signature is `List([FromQuery] TaskListQuery query)`. The `[FromQuery]` is required, because `[ApiController]` would otherwise infer `[FromBody]`.
> - `TaskListQuery.ProjectId` is `string?` with the same `[DisplayFormat(ConvertEmptyStringToNull = false)]`. The service parses it with `int.TryParse(…, NumberStyles.None, CultureInfo.InvariantCulture, out var id)`. On `""` or a non-number it adds the error `projectId: "Project id must be a whole number."`. On an unknown id it adds `"Project 42 does not exist."`. Leaving the parameter out means All.

(If keeping `int?` is preferred, state instead: "`?projectId=` is treated as All", and record it as an accepted deviation from FR-9. Do not leave it implicit.)

### L1 (low) — The frontend route table contradicts FR-9 bullet 3

> "`/projects/:id` | ProjectPage | `GET/PUT/DELETE /api/projects/{id}`, `GET /api/tasks?projectId=`"

The capability map says the project task view uses `TaskFilters`, but this row shows no status or priority filter. **Fix:** change the call to `GET /api/tasks?projectId=&status=&priority=` and add "renders `TaskFilters`". Optionally note: "PRD §8: first cut if the schedule slips."

### L2 (low) — 404 vs 400 precedence on PUT is undecided

On `PUT /api/projects/999` with an empty name, `ProjectService` could validate first and return 400, while `TaskService`, which must load the stored due date first, would return 404. The tests would then disagree. **Fix:** add to AD-2: "Update and delete load the entity by route id first. If it is missing, return `NotFound` before any validation."

### L3 (low) — Scaffold commands are missing, and the `webapi` default is minimal APIs

The memlog chooses controllers, but `dotnet new webapi` creates a minimal-API project by default. A Story 1.1 that runs the bare template must then convert it by hand. **Fix:** add a "Scaffold (Story 1.1)" block next to the README commands:

```bash
cd backend && dotnet new sln -n TaskFlow   # .slnx in .NET 10
dotnet new webapi --use-controllers -n TaskFlow.Api
dotnet new xunit -n TaskFlow.Api.Tests
dotnet sln add TaskFlow.Api TaskFlow.Api.Tests && dotnet add TaskFlow.Api.Tests reference TaskFlow.Api
cd .. && npm create vite@9.3.1 frontend -- --template react-ts
dotnet new tool-manifest && dotnet tool install dotnet-ef --version 10.0.11
```

This also lets the Stack table's "template default?" column be checked against the exact templates.

### L4 (low) — Operations dimensions are not stated: seed data, logging, DB reset

Deployment is decided well. Three small operations questions are neither decided nor deferred:

- **Seed data.** Without a decision, one story may add `HasData` demo projects, which would break AC-6 counts and the "With no tasks, all three counts show 0" check.
- **Logging.** Is the template console logging enough? Should EF SQL logging be on in Development?
- **Resetting the database.** README: "stop the API, delete `backend/TaskFlow.Api/taskflow.db*`".

**Fix:** add to Deployment & environments: "No seed data; a fresh DB is empty. Logging is the template default (console). EF command logging is left at the template level. To reset, stop the API and delete `taskflow.db*`."

### L5 (low) — The AC-5 test can pass without proving the database does the cascade

AD-6 says the delete "removes only the `Project` row (its tasks are not loaded)". But `Remove(project)` on a tracked entity also cascades to *tracked* dependents in EF. A test that creates the tasks in the same scope could then pass because of EF and not the database. Tests go through `HttpClient`, so scopes differ and the risk is low, but nothing in the rule enforces it. **Fix:** in AD-6, pin the implementation as `await db.Projects.Where(p => p.Id == id).ExecuteDeleteAsync()`, where 0 rows means `NotFound`. That way EF never sees the tasks. Optionally add one test that runs a raw `DELETE FROM Projects WHERE Id = …` through the DbContext and asserts the tasks are gone, which proves NFR-2 at the database level.

### L6 (low) — "Relative to the API project folder" is only true for the README command

> AD-10: "`Data Source=taskflow.db`, which resolves relative to the API project folder"

SQLite resolves a relative path against the process's **working directory**, not the content root. That works with `dotnet run --project TaskFlow.Api` as documented. Launching the built DLL from elsewhere, or from some IDE profiles, creates a second empty database, which looks like data loss (NFR-1). **Fix:** either state "relative to the working directory; the README command runs from the project folder", or resolve it in `Program.cs` against `builder.Environment.ContentRootPath`. Verify during the Day-3 smoke test.

### L7 (low) — Schema column lengths are undecided

AD-3 enforces 100/1,000/200/2,000 in the service, but the spine does not say whether `OnModelCreating` also sets `HasMaxLength`. SQLite ignores length, so this has no runtime effect. It does create migration churn if one story adds it later. **Fix:** add to AD-6 or the ER note: "No `HasMaxLength` in the model; lengths are enforced only by the service (AD-3)." Alternatively, "Set `HasMaxLength` to match AD-3 limits in the first migration." Pick one.

### L8 (low) — Terseness

The spine is mostly decisions, and the "Prevents" lines are required by the format. A few rationale fragments could go or move to the memlog:

- Stack/Swashbuckle: "Scalar was considered; Swagger UI is better known to reviewers."
- Stack/react-router: "(`react-router-dom` no longer exists in v8)". This one is useful as a rule; reword it as "Import from `react-router` only."
- The ER note about stored text not affecting the wire format.

The impact is minor.

---

## Part 3 — Good-spine checklist

| Check | Result | Notes |
| --- | --- | --- |
| Fixes the real divergence points for the stories that follow | ✅ mostly | Layering, result shape, validation placement, enum parsing, naming, cascade, dates and clock, PUT semantics, contract and error keys, migrations, dev topology, frontend data flow, and test harness are all pinned. Missing: the empty-string wire handling (H1, M6), 404 UI (M3), empty states (M4), HTML validation attributes and unmatched keys (M5), error-body parsing (M2), and update precedence (L2). |
| Every AD Rule is enforceable and prevents its stated divergence | ✅ with exceptions | AD-1, AD-4, AD-5, AD-7 ("nothing calls `DateTime.Now`") and AD-13 ("never use InMemory") can be checked with a grep. AD-3 does not fully prevent "two-pass errors", because of H1. AD-12's `status: 0` does not prevent blank screens, because of M2. AD-6 can be satisfied by EF alone (L5). |
| Could anything under Deferred let two units diverge? | ✅ no | Frontend tests, indexes, pagination/search, CI/HTTPS, a generated TS client (hand-written types are pinned in Conventions), concurrency (last write wins) and polish (plain CSS pinned) are all deferred safely. |
| Every dimension decided, deferred or open (incl. deployment, environments, operations) | ⚠️ | Deployment and environments are decided. Operations are partly decided: health and ProblemDetails yes; seed data, logging and DB reset no (L4). Schema lengths are undecided (L7). There is no "Open Questions" section. Add one, even if it says "None". |
| Terse (decisions, not rationale) | ✅ | Minor trims (L8). |
| Every non-template dependency listed with a reason | ✅ | SwaggerUI, EF Sqlite, EF Design, dotnet-ef, Mvc.Testing and react-router are all marked **added** with a reason. `TimeProvider`, health checks and ProblemDetails are in the box. `FixedTimeProvider` is hand-written, so there is no package. The one gap is that the template flags (`--use-controllers`) and exact template commands are not written down (L3), so "template default" cannot be checked. Frontend versions are `8.x` / `19.x` ranges rather than exact pins; `package-lock.json` will pin them, which is acceptable. |

## Suggested order of fixes

1. **H1**: decide the empty `dueDate` / `projectId` wire handling. It changes the AD-3 DTO types, so it must land before Story 1.x creates the DTOs.
2. **M6**: `[FromQuery]` and `?projectId=` handling, before the "list and filter tasks" story.
3. **M2, M3, M5**: `client.ts` and form rules, before the first frontend story.
4. **M4**: `EmptyState`, together with the first list page.
5. L1–L8, whenever the spine is next touched.
