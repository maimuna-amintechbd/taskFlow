---
title: Epics & stories validation — TaskFlow
reviewed: _bmad-output/planning-artifacts/epics.md (status: draft)
against: PRD (final), ARCHITECTURE-SPINE.md (final), CLAUDE.md
date: 2026-10-22
---

# Epics validation report

| # | Check | Result |
|---|---|---|
| 1 | Bullet-level FR coverage | **FAIL (minor)**: 5 bullets are only partly covered |
| 2 | Release checks AC-1..AC-7 with the PRD "Verified by" method | **PASS** |
| 3 | No forward dependencies; app runnable after each story | **FAIL**: 2 real forward dependencies, 2 implicit transitions |
| 4 | Entities created only when needed | **PASS** |
| 5 | Architecture compliance (AD-1..AD-14, Conventions) | **PASS** (the only AD-2 problem is the sequencing issue in check 3) |
| 6 | Sizing / day plan / cut line | **PASS with warnings**: 3.1 and 5.1 are under-estimated |
| 7 | Walking skeleton (Story 1.1) | **PASS** (one README clarification) |
| 8 | Dependencies listed in the Stack table | **PASS** |

## 1. Bullet-level coverage: FAIL (minor)

Covered by explicit ACs: FR-1 b1–b4; FR-2 b1–b3 (2.1, 2.2, 3.2); FR-3 b1–b2 (2.2); FR-4 b1–b3 (2.2, 3.4, 4.2); FR-5 b1–b7 (3.1, plus 4.1 for filters); FR-6 b1–b3 (3.1, 3.2); FR-7 b2–b5 (3.3); FR-8 b1–b2 (3.4, 4.2); FR-9 b1–b3 and b5 (3.2, 4.1); FR-10 b1 and b3 (4.2); FR-11 b1–b2 (2.1, 2.2, 3.1, 3.3).

Partly covered:

| Bullet | Gap | Belongs in |
|---|---|---|
| FR-1 b5 "Name **and description** saved trimmed" | Only whitespace→null is tested. A description with content and surrounding spaces is never tested. | 2.1 |
| FR-7 b1 "absent **or empty** description/due date is cleared" | The AC says only "null". Omitted fields and `""` are not covered. Note: per AD-3, `"dueDate": ""` is a binder 400 (`$.dueDate`). The PRD's "empty" is met only because the form sends `null` (AD-12). State that reading. | 3.3 |
| FR-9 b4 empty state when nothing matches | Asserted on the Tasks page only, not in the project task view. | 4.1 |
| FR-10 b2 "after any **create**, edit, delete or project deletion" | The listed cases are edit, delete and project delete. Create is missing. | 4.2 |
| FR-11 b3 "…**or returns an unexpected error**" | Only "API unreachable" is tested (1.1, 4.2). Nothing covers a 500 ProblemDetails, or a list page showing a load failure. | 1.1, 2.1, 3.2 |

## 2. Release checks: PASS

| AC | Story AC | Method matches PRD |
|---|---|---|
| AC-1 | 3.1 (create title rules), 3.3 (update) | automated integration ✓ |
| AC-2 | 3.1, 3.3; also 3.4 (direct FK-violation test, NFR-2) | automated integration ✓ |
| AC-3 | body: 3.1, 3.3; filters: 4.1 (`?status=7/2/''/Hgh`, `?priority=7/''`) | automated, body and filter ✓ |
| AC-4 | 3.3 manual restart check + temp-file factory A/B test; repeated in 5.1 | manual + optional automated ✓ |
| AC-5 | 3.4 (DbContext count = 0, other project untouched) | automated integration ✓ |
| AC-6 | 4.2 (zeros, 4-task mix, after change) | automated integration ✓ |
| AC-7 | 1.1 first pass; 5.1 fresh clone in ≤10 min, timed | manual fresh clone ✓ |

## 3. Forward dependencies: FAIL

| Story | Finding | Severity |
|---|---|---|
| 1.1 | README must contain "the exact run, test and build commands from the spine" and "every command was actually run". The spine's README block includes `dotnet ef migrations add …` and the note "creates taskflow.db". Neither can work before 2.1 adds EF Core, the Design package and a DbContext. | Low |
| **2.1** | The POST returns 201 with a `Location` header, and AD-2 requires `CreatedAtAction(nameof(GetById), …)`. But `GET /api/projects/{id}` (GetById) is only built in **2.2**. Without that action, `CreatedAtAction` throws "No route matches the supplied values" → **500**. | **High** |
| 2.1 | `ProjectDto.taskCount` "0 until tasks exist in Epic 3" means `ToDto` must hard-code 0, because `Project.Tasks` and `TaskItem` don't exist yet. That is OK, but neither 2.1 nor 3.1 says it, and 3.1 silently relies on `ToDto` switching to `p.Tasks.Count`. | Medium |
| **3.1** | "a valid submit returns me to the project page, where the task count has increased". 2.2 builds the project page with name and description only; no story shows the task count there. That UI is never built. | Medium |
| 3.1 | Uses `factory.Clock` and introduces `FixedTimeProvider` ✓. The `CreateTaskAsync` helper (AD-13) is never introduced explicitly, yet 3.2–4.2 rely on it. | Low |
| 3.3 | The AC-4 test needs `TaskFlowApiFactory` to accept a file connection string. The 2.1 factory is in-memory only, so 3.3 has to extend it without saying so. | Low |
| 1.1, 2.2, 3.2, 3.4, 4.1, 4.2, 5.1, 5.2 | No forward dependency. 1.1's nav links go to placeholder pages it creates. The project-name link (2.2), task-title edit link (3.3) and task Delete button (3.4) are each added by the story that builds the target. `TaskListQuery` is created in 3.2 with `ProjectId`, and 4.1 adds `Status`/`Priority`. `GET /api/tasks/{id}` exists in 3.1, so `CreatedAtAction` works for tasks. Every story leaves the app runnable (migrations apply on startup). | — |

## 4. Entities only when needed: PASS
2.1 creates `Project { Id, Name, Description? }`, explicitly without `Tasks`. 3.1 creates `TaskItem` and the enums, and "match AD-5 exactly" brings in `Project.Tasks`. No entity is created early.

## 5. Architecture compliance: PASS
- Messages match the Conventions table word for word: Name, Title, Description (1000/2000), Project is required / does not exist / whole number, Status/Priority required / must be one of, Due date.
- Keys: camelCase, plus `$.status` for a JSON number (AD-3) ✓.
- Routes and status codes match AD-9: 201/200/204/404, health returns text `Healthy` ✓.
- 404-before-400 on PUT (2.2, 3.3) ✓.
- `TaskListQuery` is a class bound `[FromQuery]` with `DisplayFormat(ConvertEmptyStringToNull=false)` (3.2, 4.1) ✓. `?status=` and `?projectId=` give 400 ✓.
- AD-8 is respected: defaults only on create, no defaults on edit, a whitespace description becomes null.
- AD-13 is respected: named shared-cache in-memory SQLite, `UseSetting`, `FixedTimeProvider`, temp file + `ClearAllPools`, no InMemory provider.
- AD-12 is respected: `noValidate`, no `min` on dates, `''` sent as null, `projectId` sent as a number, exact prompt wording and empty-state strings.
- The only conflict is the AD-2 `CreatedAtAction` → GetById ordering in 2.1, covered in check 3.

## 6. Sizing: PASS with warnings
- Day totals: 4 / 4 / 4 / 4 / 4 h = 20 h, which is ≤ 20 h (and the PRD's ≤ 28 h). At most 4 stories a day (Day 7). No story exceeds a day.
- Cut line: (1) 5.2 polish, (2) project-view filters. That matches PRD §8, and validation, tests and 5.1 are never cut ✓.
- **3.1 (3 h): unrealistic for a beginner.** It covers the first relationship and migration, two enums, `EnumNames` with unit tests, `TimeProvider` + `FixedTimeProvider`, the full `ValidateAndApplyAsync`, about 20 integration cases, and a six-field `TaskFormPage` with error display. Realistically 4–5 h. 3.2 is only 1 h, so move the two TaskFormPage ACs to 3.2 (3.1 → 2.5 h, 3.2 → 1.5 h).
- **5.1 (1 h): optimistic.** Timed fresh clone (≤10 min) + AC-1..7 checklist + 500-task perf run + keyboard UJ-1/UJ-2 in 2–3 browsers + Swagger check. That is about 1.5 h. Take the extra 0.5 h from 5.2, which is already the first cut.
- Fix 1 moves GetById into 2.1: 2.1 → 2.75 h, 2.2 → 1.25 h. Day 4 stays at 4 h.
- 1.1 (2.5 h + 1.5 h buffer) is tight but has buffer. The client key normalization could move to 2.1, where it is first observable.

## 7. Walking skeleton: PASS
1.1 scaffolds the backend (`TaskFlow.slnx`, Api), the xUnit test project and the Vite frontend. `/api/health` reaches the browser through the Vite proxy ("API connected"), with a passing `Health_Get_ReturnsHealthy` test, Swagger, the offline ErrorBanner, NotFound and the README. Clarify the README scope (Fix 4).

## 8. Dependencies: PASS
Added and listed in the Stack table: Swashbuckle.AspNetCore.SwaggerUI, Microsoft.AspNetCore.Mvc.Testing, react-router (1.1); EF Core Sqlite + Design (2.1); dotnet-ef local tool (1.1). No unlisted package appears:
- health checks, `DisplayFormat` and `TimeProvider` come with the framework or BCL;
- `FixedTimeProvider` is hand-written (no Microsoft.Extensions.TimeProvider.Testing);
- `SqliteConnection` comes transitively via EF Sqlite;
- 5.1 uses curl, and 5.2 adds no package.

## Fixes

1. **2.1 + 2.2 (High).** Move GetById to 2.1. Add to 2.1, after the POST AC: "**And** `GET /api/projects/{id}` returns 200 `ProjectDto`, or 404 `ProblemDetails` for an unknown id; the POST's `Location` header points to it (`CreatedAtAction(nameof(GetById), …)`, AD-2)." In 2.2, delete the first AC (GET by id) and drop "get-by-id 200/404" from its test list, adding it to 2.1's. Re-estimate 2.1 = 2.75 h, 2.2 = 1.25 h.
2. **2.2 (Medium; unblocks 3.1).** Replace "`/projects/:id` shows the project's name and description with Edit and Delete buttons" with "`/projects/:id` shows the project's name, description and task count, with Edit and Delete buttons".
3. **2.1 / 3.1 (Medium).** In 2.1, replace "each with `taskCount` (0 until tasks exist in Epic 3)" with "each with `taskCount`; until Story 3.1 adds `TaskItem`, `ToDto` sets `TaskCount = 0`". In 3.1, add "**And** `Project.Tasks` is added and `ProjectService.ToDto` now computes `TaskCount = p.Tasks.Count()`."
4. **1.1 / 2.1 (Low).** In 1.1, replace "the exact run, test and build commands from the spine" with "the exact run, test and build commands from the spine (the developer-only `dotnet ef migrations add` line and the 'creates taskflow.db' note are added in Story 2.1)". In 2.1, add "**And** the README gains the `dotnet tool restore && dotnet ef migrations add …` command, which was used to create this migration."
5. **1.1, 2.1, 3.2 (FR-11 b3).** Add to 1.1: "**And** a non-2xx JSON response (e.g. a 500 `ProblemDetails`) throws `ApiError` whose `message` is the body's `title`, shown in the `ErrorBanner`." Add to 2.1 and 3.2: "If loading the list fails, the page shows the `ErrorBanner` instead of the list."
6. **4.2 (FR-10 b2).** Replace "after editing a task to `Done`, deleting a task, or deleting a project with tasks" with "after creating a task, editing a task to `Done`, deleting a task, or deleting a project with tasks".
7. **4.1 (FR-9 b4).** In the project-page AC, append: "and shows "No tasks match these filters." when nothing matches."
8. **2.1 (FR-1 b5).** Add: "**And** `"description": "  My notes  "` is saved as "My notes"."
9. **3.3 (FR-7 b1).** Replace "a null description or due date clears the stored value" with "an omitted, null, empty or whitespace-only description, and an omitted or null due date, clear the stored value; in the UI an emptied due-date input is sent as `null` (AD-12)".
10. **3.1 (AD-13).** Add: "**And** `FixedTimeProvider` (`factory.Clock`) and the `CreateTaskAsync` helper are added to `TestInfrastructure/`."
11. **3.3 (Low).** Add to the AC-4 test AC: "`TaskFlowApiFactory` gets an optional connection-string constructor parameter for this test."
12. **Sizing.** Move 3.1's two TaskFormPage ACs ("Given the project page…" and "Given I submit the form…") to 3.2. Set 3.1 = 2.5 h and 3.2 = 1.5 h. Set 5.1 = 1.5 h and 5.2 = 0–1 h. Day 7 = 4.1 (1.5) + 4.2 (1) + 5.1 (1.5) = 4 h, with 5.2 only if time remains.
13. **3.2 (optional).** Add a tie-break case: "two tasks with the same `createdAt` are ordered by id descending".
