---
title: TaskFlow
status: final
created: 2026-10-21
updated: 2026-10-22
---

# PRD: TaskFlow

## 0. Document Purpose

This PRD defines the MVP of TaskFlow for its builder (Maimuna) and for the BMAD workflows that come next (architecture, then epics and stories). Features are grouped in §4. Each functional requirement (FR) is written as a user story and has testable acceptance criteria. Terms in §3 are used exactly as defined there. Decisions first inferred by the PM and later confirmed by the builder are listed in §11. Technical decisions (stack conventions, API shape, run commands) are in [addendum.md](addendum.md), not here.

## 1. Problem and Vision

**Problem.** People working on several small projects at once (courses, side projects, freelance jobs) keep their tasks in sticky notes, chat messages and plain text files. Tasks get lost, nothing records what is important or due, and it is hard to answer a simple question: *how much is left?*

**Vision.** TaskFlow is a small web app that runs locally. It groups tasks under projects and gives each task a status, a priority and an optional due date. A dashboard shows at a glance how much is done and how much is still pending. The app checks every input and explains clearly what is wrong. It is also a portfolio piece: a reviewer should be able to clone the repository, start the app with the documented commands, and see a clean, tested full-stack design (React + ASP.NET Core + EF Core + SQLite).

## 2. Target User

**Primary user:** one person managing their own tasks across a handful of projects (tens to a few hundred tasks). There is no team and no sharing.

**Secondary audience:** a portfolio reviewer (for example a hiring manager or mentor) who runs the app locally and reads the code and tests.

### 2.1 Jobs To Be Done
- Capture a task quickly, under the project it belongs to.
- See what to work on next, by narrowing tasks down to status and priority.
- Know how much work is left overall without counting by hand.
- Trust that data is still there tomorrow and that bad input is caught, not silently saved.

### 2.2 Key User Journeys
- **UJ-1. Aisha plans her week.** Aisha, a bootcamp student with two side projects, opens TaskFlow, creates the project "Portfolio site" and adds three tasks, one High priority and due Friday. She filters the task list to *High* + *Todo* to choose what to start, sets that task to *InProgress*, and checks the dashboard: 3 total, 0 completed, 3 pending. **Edge case:** she picks yesterday's date by mistake. The form tells her the due date cannot be in the past, and nothing is saved.
- **UJ-2. Aisha cleans up.** The "Portfolio site" project is finished. She deletes it, confirms a prompt saying its tasks will also be deleted, and the dashboard totals drop to match.
- **UJ-3. A reviewer runs it.** Sam clones the repository, follows the README to start the backend and the frontend, creates a project and a task, restarts the backend, and finds the data still there. Sam also runs the backend tests and sees them pass.

## 3. Glossary
- **Project** — A named container for tasks. A project has zero or more tasks.
- **Task** — A unit of work. It belongs to exactly one project. (Named `TaskItem` in code; see addendum.)
- **Status** — The state of a task: exactly one of `Todo`, `InProgress`, `Done`.
- **Priority** — The importance of a task: exactly one of `Low`, `Medium`, `High`.
- **Due date** — Optional calendar date (no time of day) by which the task should be done.
- **Today** — The current calendar date in the backend machine's local time zone.
- **Completed task** — A task with status `Done`.
- **Pending task** — A task with status `Todo` or `InProgress`.
- **Dashboard** — A view showing the counts of all tasks, completed tasks and pending tasks across all projects.
- **Validation error** — A rejected request, with a message naming the field and the rule it broke.

## 4. Features

### 4.1 Projects

**Description:** The user creates, views, edits and deletes projects. The project list shows each project's name and its number of tasks. Opening a project shows its tasks (FR-2), which can be filtered (FR-9). Realizes UJ-1, UJ-2.

#### FR-1: Create a project
As the user, I want to create a project so that I can group related tasks.
- Name is required: after trimming spaces it must be 1–100 characters.
- Description is optional, up to 1,000 characters.
- If the input is invalid, nothing is saved and a validation error is shown for each failing field.
- Project names do not need to be unique.
- Name and description are saved with leading and trailing spaces removed.

#### FR-2: View projects
As the user, I want to see my projects and open one so that I can find its tasks.
- The list shows every saved project with its name and task count. With no projects, an empty-state message invites the user to create one.
- Opening a project shows its name, description and tasks (the same columns and order as FR-6). With no tasks, an empty-state message is shown. This is the **project task view**.
- Requesting a project that does not exist returns "not found" (HTTP 404), and the UI says so.

#### FR-3: Edit a project
As the user, I want to change a project's name or description.
- The same rules as FR-1 apply. Changes are saved and still there after a backend restart.
- Editing a project that does not exist returns HTTP 404.

#### FR-4: Delete a project and its tasks
As the user, I want to delete a project together with all its tasks so that finished work does not clutter my list.
- The UI asks for confirmation and says how many tasks will also be deleted.
- After deletion, neither the project nor any of its tasks can be retrieved, and the dashboard counts no longer include them.
- The database itself enforces the deletion of the tasks (a cascade rule on the relationship), not only application code.

### 4.2 Tasks

**Description:** The user creates, views, edits and deletes tasks. Each task belongs to one project. Realizes UJ-1.

#### FR-5: Create a task
As the user, I want to add a task to a project so that I can track the work.
- **Title** is required: after trimming spaces it must be 1–200 characters. A missing, empty or whitespace-only title is rejected. The title is saved trimmed.
- **Project** is required and must be an existing project. A missing or unknown project is rejected with a validation error.
- **Status** defaults to `Todo`. **Priority** defaults to `Medium`.
- Status and priority accept only the exact names in §3, matched without regard to upper or lower case (`high` = `High`). Everything else is rejected with a validation error. That includes numbers of any kind (`7` and even `2`), misspellings and empty strings. The same rule applies in request bodies and in filter parameters (FR-9).
- **Due date** is optional. If given, it must be today or later; a date before today is rejected.
- **Description** is optional, up to 2,000 characters.
- On success the task is saved and returned with its assigned id.

#### FR-6: View tasks
As the user, I want to see my tasks and the details of each one.
- Each task shows its title, project, status, priority and due date (if any).
- Tasks are listed newest first, by creation time (recorded automatically when the task is created).
- Requesting a task that does not exist returns HTTP 404.

#### FR-7: Edit a task
As the user, I want to change any field of a task, including moving it to another project and changing its status.
- An edit sends the whole task. Title, project, status and priority are all required on edit; there are no defaults, so an edit never silently resets a field. A description or due date that is absent or empty is cleared.
- The title, project, status, priority and description rules from FR-5 apply.
- Due date on edit: an unchanged due date is accepted even if it is now in the past. A *new* due date before today is rejected. Clearing the due date is allowed.
- Changes are still there after a backend restart.
- Editing a task that does not exist returns HTTP 404.

#### FR-8: Delete a task
As the user, I want to delete a single task.
- The UI asks for confirmation. After deletion the task cannot be retrieved, and the dashboard counts update.
- Deleting a task that does not exist returns HTTP 404.

### 4.3 Filtering

#### FR-9: Filter tasks by status and priority
As the user, I want to narrow the task list by status and/or priority so that I can see what to work on next. Realizes UJ-1.
- The user can filter by status, by priority, or by both. Both filters together mean the task must match both.
- Each filter has an "All" option that turns it off.
- The same filters work inside the project task view (FR-2).
- If no task matches, an empty-state message is shown.
- An invalid filter value sent to the API is rejected with a validation error, not silently ignored. This includes an invalid status or priority (FR-5 rule) and an unknown project id.

### 4.4 Dashboard

#### FR-10: Show task totals
As the user, I want to see how many tasks I have in total, how many are completed and how many are pending, so that I know how much is left. Realizes UJ-1, UJ-2.
- **Total** = all saved tasks. **Completed** = tasks with status `Done`. **Pending** = tasks with status `Todo` or `InProgress`. Total always equals completed + pending.
- Counts are calculated from saved data each time the dashboard is opened or reloaded. After any create, edit, delete or project deletion, the next time the dashboard is opened it shows the new numbers. It does not update live.
- With no tasks, all three counts show 0.

### 4.5 Validation and error feedback

#### FR-11: Useful error messages
As the user, I want clear messages when something is wrong so that I can fix it.
- The backend validates every write and never relies on the frontend alone. Invalid requests return HTTP 400 with a message per failing field, naming the field and the rule (e.g. "Title is required", "Due date cannot be in the past").
- The frontend shows field messages next to the matching input and keeps what the user typed.
- If the backend cannot be reached, or returns an unexpected error, the UI shows a general error message instead of failing silently or showing a blank screen.

## 5. Non-Functional Requirements
- **NFR-1 Persistence:** All data is stored in a SQLite database file on disk. Every saved change is still there after the backend restarts.
- **NFR-2 Data integrity:** The database enforces that every task has an existing project, and that deleting a project deletes its tasks.
- **NFR-3 Local run:** The README documents the prerequisites and the exact commands to set up, run and test the app. The database is created automatically on first run; there is no manual SQL step.
- **NFR-4 Cost:** Only free, open-source tools and libraries. No paid or cloud services.
- **NFR-5 Testability:** Automated backend tests (xUnit) cover every validation rule and the cascade delete. Integration tests use a real SQLite database (in-memory), so that foreign keys and cascades behave as in production.
- **NFR-6 API documentation:** When the backend runs locally in development, its API is browsable through an OpenAPI/Swagger page.
- **NFR-7 Responsiveness:** With up to 500 tasks on a typical laptop, lists, filters and the dashboard load in under 1 second.
- **NFR-8 Usability:** Works in current desktop Chrome, Firefox and Edge. Form inputs have labels, and every action can be done with the keyboard.
- **NFR-9 Code clarity:** Code a beginner can read. HTTP handling, business rules and data access are kept apart; no layers or patterns without a clear need.

## 6. Out of Scope (MVP)
- Logins, user accounts, multiple users, sharing or permissions.
- Deployment to a server or the cloud. Mobile apps. Offline or installable app (PWA).
- Real-time updates, notifications, reminders, emails.
- Subtasks, tags or labels, comments, attachments, recurring tasks, time tracking.
- Text search, custom sorting, drag-and-drop or Kanban boards.
- Per-project dashboards, charts, overdue counts, reports. *(Per-project counts are the first candidate for v2.)*
- Import/export, undo, archiving, soft delete.
- Pagination; MVP volumes (NFR-7) fit on one page.
- Handling simultaneous edits from several browser tabs: the last save wins.
- Localization; English only.

## 7. Goals and Success Metrics
**Goals:** ship a working, tested MVP in 7 days (3–4 h/day); learn ASP.NET Core and EF Core through it; end with a portfolio piece someone else can run.

- **SM-1:** All 7 release acceptance checks in §10 pass on Day 7. Each is shown by the method in its "Verified by" column.
- **SM-2:** With the prerequisites installed, a fresh clone runs in **≤ 10 minutes** using only the README. Validates NFR-3.
- **SM-3:** `dotnet test` passes, with at least one test for each rule in FR-1, FR-4, FR-5, FR-7, FR-9 and FR-10. Validates NFR-5.
- **SM-4:** Total build effort is ≤ 28 hours, and no story takes more than one day. Validates scope.
- **SM-C1 (counter-metric):** Do not chase feature count or test-coverage percentage. Adding features beyond §4, or writing tests that only raise coverage, works against SM-4 and code clarity (NFR-9).

## 8. Risks
| Risk | Mitigation |
|---|---|
| ASP.NET Core and EF Core are new to the builder, and learning slows delivery. | Build one small story at a time with standard templates and minimal layers. Explain each concept as it comes up. |
| Undefined enum values (e.g. `"priority": 7`) are accepted silently. By default .NET binds numbers to enums without checking them. | Serialize enums as strings and explicitly reject undefined values. Add tests for both. |
| Cascade delete or foreign keys are not enforced in tests (the EF InMemory provider ignores them). | Integration tests run on real SQLite in-memory (NFR-5). |
| Off-by-one errors around "today" for due dates (time zones, date versus date-and-time). | Due date is a date only, compared with today on the backend. Tests cover yesterday, today and tomorrow. |
| Dev setup issues (CORS, ports, HTTPS certificates) eat a day. | Settle ports and CORS in the architecture step. Smoke-test the skeleton on Day 3. |
| Scope creep beyond §4. | §6 is binding. New ideas go to a v2 list. |
| The schedule slips. | Cut UI polish first, then filters inside the project task view (FR-9, bullet 3). Never cut validation or tests. |

## 9. Definition of Done
**Per story:** acceptance criteria met; backend builds without errors; `dotnet test` passes; the frontend type-checks and builds (`npm run build`); new rules have tests; the behaviour has been checked by hand in the browser; committed with a clear message.

**MVP release:** every story done; all §10 checks pass; the README is verified on a fresh clone (SM-2); Swagger page works; no known bugs that lose data or crash the app.

## 10. Release Acceptance Checks
| # | Check | Covered by | Verified by |
|---|---|---|---|
| AC-1 | A task must have a valid title. | FR-5, FR-7 | Automated integration tests |
| AC-2 | A task must reference a valid project. | FR-5, FR-7, NFR-2 | Automated integration tests |
| AC-3 | Invalid task status and priority values are rejected. | FR-5, FR-7, FR-9 | Automated integration tests (body and filter parameters) |
| AC-4 | Task updates persist after restarting the backend. | FR-7, NFR-1 | Manual check: edit a task, stop and restart the backend, reload; the change is still there. Optionally, one automated test using a temporary SQLite file. |
| AC-5 | Deleting a project deletes all its tasks (cascade). | FR-4, NFR-2 | Automated integration tests |
| AC-6 | Dashboard totals reflect saved task data. | FR-10 | Automated integration tests |
| AC-7 | The app runs locally using documented commands. | NFR-3, SM-2 | Manual check: fresh clone, follow the README |

## 11. Confirmed Decisions
*Inferred while drafting; confirmed by Maimuna on 2026-10-22.*
- §2: Primary user manages tens to a few hundred tasks.
- §2.2: UJ-1 was drafted by the PM and accepted by the builder.
- §3: "Today" is the date in the backend machine's local time zone (the backend and the browser are on the same machine).
- FR-1: Project name 1–100 characters; description ≤ 1,000 characters; names need not be unique.
- FR-5: Title ≤ 200 characters; description ≤ 2,000 characters; default status `Todo`, default priority `Medium`; status and priority names are matched without regard to case.
- FR-6: Tasks listed newest first.
- FR-9: Filters also apply inside the project task view.
- NFR-7: Under 1 s with up to 500 tasks.
- NFR-8: Supported browsers and basic keyboard access.

## 12. Open Questions
None blocking. Exact routes, data shapes and the folder layout are left to the architecture step (see addendum).
