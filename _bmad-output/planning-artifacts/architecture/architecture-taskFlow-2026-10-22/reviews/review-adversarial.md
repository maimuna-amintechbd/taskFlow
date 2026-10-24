---
review: adversarial (divergence pairs)
target: ../ARCHITECTURE-SPINE.md
date: 2026-10-22
lens: "Build two units one level down that obey every AD to the letter yet still build incompatibly."
---

# Adversarial review — TaskFlow architecture spine

## Verdict

**Sound spine, needs tightening before stories are cut.** The hard parts are pinned and pinned well: enum parsing, cascade, migrations, ports and the test DB. No finding is a broken paradigm.

The holes sit where the spine names a concept but leaves its **shape** open: entity nullability, the `ServiceResult` type, how create and update share validation, how a write returns its DTO, what the form sends for an empty input, and the test clock. Those are exactly the places a Day-2 session and a Day-4 session will fill in differently.

There are **4 high, 8 medium and 6 low** findings. Two ADs contradict each other or cannot be enforced as written (H1, M5). Nothing in the spine pushes toward over-engineering. A few fixes below are deliberately the *simpler* option.

Each finding below gives the two units, how each one obeys the ADs, the clash, the severity, and the fix text for the AD.

---

## High

### H1 — Empty form inputs vs. typed DTO fields (AD-8 and AD-12 contradict AD-3)

- **Unit A, Story "Create task" (frontend).** It obeys AD-12: "Dates stay `yyyy-MM-dd` strings from `<input type="date">` to the API." An untouched date input is `""`, so the body sends `"dueDate": ""`. It also obeys AD-8: "A null or **empty** … due date clears it." The project `<select>` placeholder sends `"projectId": ""`.
- **Unit B, Story "Create task" (backend).** It obeys AD-3: `DueDate` is `DateOnly?` and `ProjectId` is `int?`.
- **Clash.** System.Text.Json cannot turn `""` into `DateOnly?`, or `""` into `int?` (even with Web number handling). So every task created without a due date returns the framework's automatic 400. That 400 has a technical message under `$.dueDate`, plus a stray `"request": ["The request field is required."]` key. FR-5's main path is broken, and AD-8's phrase "empty due date clears it" cannot happen on the backend. Each later story fixes this differently: one sends `null`, one changes the DTO to `string?`, one adds a custom converter.
- **Fix: tighten AD-8 and AD-12.**
  > AD-8: "…A **null** description or due date clears it; an empty-after-trim description is stored as null." (Delete "or empty … due date".)
  > AD-12: "Form state holds strings. Before sending, the page's submit handler converts `''` to `null` for `dueDate`, `description` and `projectId`, and converts `projectId` to a number. The API never receives `""` for a typed field."

### H2 — Two mapping paths for one response DTO (Conventions vs. AD-9)

- **Unit A, Story "List/view tasks".** It obeys Conventions: "Reads use `AsNoTracking()` and project with `Select` straight into DTOs."
- **Unit B, Story "Edit project" / "Create task".** It obeys AD-1 ("mapping is written by hand in the service"). After `SaveChangesAsync`, it maps the tracked entity to the DTO by hand.
- **Clash.** It shows up in three places:
  - `PUT /api/projects/{id}` returns `ProjectDto.taskCount = 0`, because `project.Tasks` was never loaded. ProjectPage stores that response, and the AD-12 delete confirmation then says **"0 tasks will be deleted"** while it deletes 5. That silently breaks FR-4, and no backend test catches it.
  - `POST /api/tasks` needs `projectName`, so it needs an extra load.
  - `createdAt` comes from the in-memory entity as `Kind=Utc` with a `Z` suffix, but from SQLite as `Kind=Unspecified` with no `Z`. The same resource gets two wire formats.
- **Fix: add to AD-1.**
  > "Each service has **one** private static method `ToDtos(IQueryable<Entity>)` that holds the only `Select` into the response DTO. Lists and gets call it. After a successful create or update, the service returns the DTO by re-querying through the same method (`ToDtos(db.X.Where(x => x.Id == id)).SingleAsync()`). Entities are never mapped to response DTOs by hand."

  This stays simple. It is a plain method, with no `Expression<>` fields and no mapper.

### H3 — Create and update validation written twice (AD-3 "one validation method per write")

- **Unit A, Story "Create task" (Day 2).** It obeys AD-3: `ValidateCreateAsync` checks title, description, projectId and enums, and applies defaults.
- **Unit B, Story "Edit task" (Day 3).** It obeys AD-3, because "each write has one validation method". It writes its own `ValidateUpdateAsync` with the due-date-unchanged rule.
- **Clash.** The FR-5 rules (trim, length limits, enum parsing, project exists) now live in two copies, and they drift. For example, update forgets to trim, or keeps an empty description as `""` instead of null. FR-7 says "the FR-5 rules apply", but the spine allows two copies. The same split happens for projects (FR-1 and FR-3).
- **Fix: tighten AD-3.**
  > "Each service has **one** method per entity, `ApplyAsync(Save*Request req, Entity target, bool isNew)`. It validates every field into one error dictionary and, only if the dictionary is empty, copies the trimmed and parsed values onto `target`. It returns the dictionary. `CreateAsync` and `UpdateAsync` both call it. `isNew` switches only the defaults (AD-8) and the due-date rule (AD-7). Record **one message per field**: the first rule that fails."

  The last sentence also settles whether a field can carry several messages.

### H4 — Entity and TS type nullability not pinned (AD-5, ER diagram)

- **Unit A, Story "Project model + migration".** It obeys AD-5 and the ER diagram (`string Description`). It writes `public string Description { get; set; } = "";`, which is the usual beginner habit for silencing nullable warnings. EF makes the column `NOT NULL`.
- **Unit B, Story "Create project".** It obeys AD-8: "A description that is empty after trimming is stored as null."
- **Clash.** `SaveChanges` fails a NOT NULL constraint and returns a 500 for every project without a description. If Unit A is "fixed" by storing `""`, the wire format then sends `""` in some places and `null` in others. The frontend has the same split: one story writes `description?: string` and another writes `description: string | null`. A `null` fed into `<input value>` causes React's controlled/uncontrolled warning.
- **Fix: add the exact shapes to AD-5.**
  > `Project { int Id; string Name = ""; string? Description; List<TaskItem> Tasks = []; }`
  > `TaskItem { int Id; string Title = ""; string? Description; TaskItemStatus Status; TaskPriority Priority; DateOnly? DueDate; DateTime CreatedAt; int ProjectId; Project Project = null!; }`
  >
  > "`types.ts` mirrors nullable C# members as `T | null` (never as optional `?`). Response DTO properties are always present. Forms turn `null` into `''` when they load data into form state."

  Also mark `Description` and `DueDate` as nullable in the ER diagram.

---

## Medium

### M1 — FixedTimeProvider: local time zone and a frozen clock (AD-7 × AD-13)

- **Unit A, Story "Test infrastructure".** It obeys AD-13 with a ~10-line `FixedTimeProvider` that overrides `GetUtcNow()` only and stays frozen.
- **Unit B, Story "Edit task", test "unchanged past due date accepted".** It needs a stored due date that is now in the past. It either seeds the database directly or makes the clock settable, and it edits the shared class to do that.
- **Clash.** It has two parts:
  - The service uses `GetLocalNow()`. With only `GetUtcNow` overridden, `LocalTimeZone` stays `TimeZoneInfo.Local`. Tests whose fixed instant is close to midnight UTC then pass on Maimuna's machine and fail on a reviewer's machine in another zone.
  - Tests compute "yesterday" and "tomorrow" from different constants.
- **Fix: tighten AD-13.**
  > "`FixedTimeProvider` overrides `GetUtcNow()` and `LocalTimeZone => TimeZoneInfo.Utc`. It has a settable `UtcNow` that defaults to `2026-06-15T12:00Z` and exposes `Today`. The factory exposes it as `factory.Clock`. Tests build dates only from `factory.Clock.Today` (`AddDays(±1)`). To test 'unchanged past date', create the task, then move `factory.Clock.UtcNow` forward."

### M2 — Tests deserialize enum DTOs differently (AD-13)

- **Unit A, Projects tests.** They read responses into `JsonElement` or the API's `ProjectDto`, which has no enums, so this works.
- **Unit B, Tasks tests.** They call `ReadFromJsonAsync<TaskDto>()`. The client-side Web defaults have no `JsonStringEnumConverter`, so `"status":"Todo"` throws `JsonException`. The developer then adds a converter locally, or rewrites `TaskDto.Status` as `string`. That second option is legal under AD-4, which only says "responses send names".
- **Clash.** Test setup now varies by test class, and an API DTO type may change only to suit the tests. On top of that, every test class grows its own `CreateProjectAsync` helper with different defaults.
- **Fix: add to AD-13.**
  > "`TestInfrastructure/` contains `ApiJson.Options` (Web defaults + `JsonStringEnumConverter`) and `ApiClientExtensions` with `CreateProjectAsync(name = "P")` and `CreateTaskAsync(projectId, title = "T", …)`, which POST through HTTP. Every test uses these. Tests read responses into the API's own DTO types and never redefine them."

### M3 — `CreatedAtAction` and the `Async` action-name suffix (AD-2)

- **Unit A, ProjectsController.** Actions are named `Get`/`Create`, and it returns `CreatedAtAction(nameof(Get), …)`. This works.
- **Unit B, TasksController.** Following the Conventions line "every EF call is `*Async`", actions are named `GetAsync`/`CreateAsync`, and it returns `CreatedAtAction(nameof(GetAsync), …)`.
- **Clash.** MVC strips `Async` from action names (`SuppressAsyncSuffixInActionNames`), so `POST /api/tasks` returns a **500** ("No route matches the supplied values"). This is a classic trap, and the spine's own wording points toward it.
- **Fix: add to Conventions (C# naming).**
  > "Controller actions have **no** `Async` suffix (`List`, `Get`, `Create`, `Update`, `Delete`). Service methods keep it (`ListAsync`, …)."

### M4 — `ServiceResult` shape and controller mapping style unpinned (AD-2)

- **Unit A, Projects story.** It writes `ServiceResult<T>` as a record with `IsSuccess`, `Value` and `Errors`, and maps it inline with `if` chains in each action.
- **Unit B, Tasks story.** It needs the non-generic `ServiceResult` for delete. It adds that with a `Kind` enum and writes a `ToActionResult(this ControllerBase, …)` extension, which nothing in the spine forbids.
- **Clash.** The result ends up with two shapes and two mapping styles across three controllers, and the spine never says which style is the "unnecessary abstraction".
- **Fix: pin the type in AD-2.** The code is about 20 lines, all in `Services/ServiceResult.cs`.
  > ```csharp
  > public enum ResultKind { Ok, NotFound, Invalid }
  > public class ServiceResult {           // also used for "no value" (delete)
  >     public ResultKind Kind { get; init; }
  >     public Dictionary<string, string[]> Errors { get; init; } = new();
  >     public static ServiceResult Ok() => new() { Kind = ResultKind.Ok };
  >     public static ServiceResult NotFound() => new() { Kind = ResultKind.NotFound };
  >     public static ServiceResult Invalid(Dictionary<string, string[]> e) => new() { Kind = ResultKind.Invalid, Errors = e };
  > }
  > public class ServiceResult<T> : ServiceResult { public T? Value { get; init; } /* same three factories */ }
  > ```
  > "Controllers map it inline with one `switch` on `Kind` per action. There is no base controller and no extension method."

### M5 — Unenforceable error-display rules in AD-12 (status 0; unmatched keys; `message`)

- **Unit A, TaskFormPage.** It shows `fieldErrors[key]` next to inputs, and the banner only on non-400 errors.
- **Unit B, TasksPage.** It has no form. `?status=foo` in the URL returns a 400 whose only content is `fieldErrors.status`. The page shows `err.message` in the banner, which reads "One or more validation errors occurred." and gives no reason.
- **Clash (a): unmatched keys.** AD-12's "any other error" does not say whether a 400 field key that has no matching input counts. Examples are `status` on TasksPage, or the binder's `request` key. Pages decide differently, and some drop the message silently, which FR-11 forbids.
- **Clash (b): status 0 is unreachable.** AD-11 sends every call through the Vite proxy. When the API is down, Vite answers **500 with an empty text body**, so fetch never fails. The "status 0 / Cannot reach the server" branch in AD-12 can never fire in the only topology there is.
- **Clash (c): `message` has no defined source.** It could come from `title`, from `detail`, or from a status text.
- **Fix: tighten AD-12.**
  > "`client.ts` sets `message` from the ProblemDetails `title`. If the body is not JSON, or fetch throws, it sets `status: 0` and the message 'Cannot reach the server.' (The Vite proxy returns a non-JSON 500 when the API is down.) Every page passes an `ApiError` to `ErrorBanner`. The banner lists every `fieldErrors` entry whose key has no input on that page, so no message is ever dropped."

### M6 — Delete-confirmation count source (AD-12 × FR-4 × FR-9)

- **Unit A, Project delete.** It obeys AD-12 ("states the project's `taskCount`") using `project.taskCount` from the first load.
- **Unit B, Task delete on ProjectPage.** After a delete it removes the row from local state. The developer, seeing the stale count, switches the confirmation to `tasks.length`.
- **Clash.** `taskCount` is stale after an in-page task delete (and after H2's PUT bug). `tasks.length` counts only the filtered list once FR-9 filters are active. Either way, the destructive prompt shows a wrong number.
- **Fix: add to AD-12.**
  > "After any create, update or delete, a page re-fetches its data. It does not patch its local state. The project-delete confirmation uses `taskCount` from the project as last re-fetched, never `tasks.length`."

### M7 — HTML constraint attributes (`min`, `required`, `maxLength`) vs. server-only rules

- **Unit A, TaskFormPage in create mode.** It adds `min={today}` to the date input, which is helpful and allowed.
- **Unit B, TaskFormPage in edit mode, a later story.** It reuses the same input.
- **Clash.** The browser **blocks submit** of an unchanged past due date, which FR-7 and AD-7 explicitly allow. Computing `today` in JS (`toISOString()`) also gives the UTC date, which goes against AD-12's "never convert to JS `Date`". `required` and `maxLength` used on some forms but not others give inconsistent behavior, and they hide the server messages that FR-11 tests by hand.
- **Fix: add to AD-12.**
  > "Forms use `noValidate` and no `required`, `min`, `max` or `maxLength` attributes. Every rule and message comes from the API (AD-3)."

### M8 — Positional vs. property records; `[DisplayFormat]` on `TaskListQuery` (AD-4 × Conventions)

- **Unit A.** `public record TaskListQuery(string? Status, string? Priority, int? ProjectId);` with `[DisplayFormat(...)]` on the constructor parameters. It obeys the convention "DTOs are `record`s".
- **Unit B.** `SaveTaskRequest` written with `{ get; init; }` properties.
- **Clash.** On a positional record, an attribute on the parameter does not land on the property. That leaves it unclear (and untested) whether MVC metadata sees it. If it doesn't, `?status=` is silently treated as "All", which breaks FR-9 ("rejected, not ignored"). AD-4 also calls `TaskListQuery` a "class", while Conventions call it a record.
- **Fix: tighten Conventions.**
  > "Request DTOs and `TaskListQuery` are `record`s with `{ get; init; }` properties. Attributes go on properties. Response DTOs are positional records. `TaskListQuery.ProjectId` is `int?`. Required test: `GET /api/tasks?status=` → 400 with key `status`."

---

## Low

### L1 — Validation message wording beyond the four examples

Projects might say "Name is required." while tasks say "Project name is required."; one service writes "1,000" and the other "1000"; a missing project could read "Project is required." or "ProjectId is required.". Tests assert keys, not messages, so the damage is cosmetic, but it is visible to a portfolio reviewer.

**Fix:** Add one row per rule to the Errors convention, covering Name/Title/Description/Project/Status/Priority/Due date × required/length/unknown/past. Field labels: `Name`, `Title`, `Description`, `Project`, `Status`, `Priority`, `Due date`. Numbers are written without separators.

### L2 — Frontend API surface and enum option lists

The TasksPage story writes `listTasks(status?, priority?)`, and the ProjectPage story then adds a third positional parameter or a new `listProjectTasks`. Each select hard-codes its own options, with labels such as "In progress" vs. "InProgress".

**Fix:** "`types.ts` exports `TASK_STATUSES = ['Todo','InProgress','Done'] as const` (and the same for priorities), with types derived from them. Every select iterates these lists. `tasks.ts` exports `listTasks(filters: { status?, priority?, projectId? })`, `getTask`, `createTask`, `updateTask` and `deleteTask`, and `projects.ts` mirrors that."

### L3 — Components calling the API

One story's `TaskTable` calls `deleteTask` itself, while another page expects an `onDelete` callback so it can re-fetch.

**Fix:** "Only pages import from `src/api/`. Components get data and callbacks through props."

### L4 — Second mutation path for status (inline select)

UJ-1 ("sets that task to InProgress") invites an inline status dropdown in `TaskTable`. That would build a `SaveTaskRequest` from a `TaskDto` in a second place, or tempt someone into a PATCH endpoint, which AD-9 forbids.

**Fix:** "Status and every other task field change only through `TaskFormPage`; there is no inline editing in the MVP."

### L5 — 404 on a page's main resource

ProjectPage might show `ErrorBanner("Not Found")` while TaskFormPage renders a not-found view. FR-2 only requires that the UI "says so".

**Fix:** "A 404 on the page's own resource renders `<NotFoundPage />` in place of the page."

### L6 — Test DB override is unverified, and pools hold the file

If `UseSetting("ConnectionStrings:TaskFlow", …)` were ever shadowed by `appsettings.json`, tests would migrate and write a real `taskflow.db` without any error. Separately, the AC-4 file test can't delete its temp file while pooled connections hold it.

**Fix:** "The factory asserts on startup that the resolved connection string is the one it injected. The AC-4 test calls `SqliteConnection.ClearAllPools()` before deleting the file."

*(Not raised: project-list ordering by name uses SQLite's binary collation, so "alpha" sorts after "Zeta". One developer keeps that consistent; state it, or use `NOCASE`, if it bothers you.)*

---

## Contradictory or unenforceable ADs

| AD | Problem | Fixed by |
| --- | --- | --- |
| AD-8 ↔ AD-3/AD-12 | "Empty due date clears it" cannot reach a `DateOnly?` DTO. The form sends exactly that `""`. | H1 |
| AD-12 ↔ AD-11 | The `status: 0` "Cannot reach the server" branch never fires behind the Vite proxy. | M5 |
| AD-3 ↔ AD-4 | "Do not use DataAnnotations" vs. the required `[DisplayFormat]` from `System.ComponentModel.DataAnnotations`. | Reword AD-3: "Do not use DataAnnotations **validation** attributes (`[Required]`, `[StringLength]`, `[Range]`…)." |
| Conventions ↔ AD-1 | "Reads project with `Select`" + "map by hand" gives two paths for every DTO. | H2 |
| AD-7 ↔ AD-13 | `GetLocalNow()` under a test clock depends on the machine's time zone. | M1 |
| AD-4 ↔ Conventions | `TaskListQuery` is called a "class" in one place and a "record" in the other. | M8 |

## Over-engineering check

Nothing in the spine pushes a beginner toward needless abstraction. Repositories, AutoMapper, FluentValidation, interfaces and a global store are all explicitly ruled out.

Two cautions about the fixes above:

- H2 should stay a plain private method. Do not let it become `Expression<Func<…>>` fields or a mapping class.
- M4 should stay inline `switch` statements. Do not build a base controller.

The mandated automated AC-4 test is slightly more than the PRD requires ("optionally"), but it is cheap and worth keeping.
