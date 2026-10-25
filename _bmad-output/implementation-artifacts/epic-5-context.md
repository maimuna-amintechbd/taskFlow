# Epic 5 Context: Portfolio-ready release

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Turn the finished MVP (projects, tasks, filters, dashboard from Epics 1–4) into something a portfolio reviewer can trust. On a fresh clone the README alone should get the app running. Every release acceptance check should be proven, with evidence recorded. Data should survive a backend restart. Lists, filters and the dashboard should stay fast with 500 tasks, and the UI should be tidy and fully keyboard-usable. This epic mostly verifies and records evidence. It adds almost no new behavior: the release check is never cut, and the UI polish is the first thing cut if time runs short.

## Stories

- Story 5.1: Release check — fresh clone, all acceptance checks, performance
- Story 5.2: UI polish and keyboard pass ✂️ (first to cut)

## Requirements & Constraints

- **Fresh-clone run:** with the prerequisites installed (.NET 10 SDK, Node.js 22.22+), a fresh `git clone` must run in **≤ 10 minutes** using only the README. That covers starting the API and the frontend, creating a project and a task, `dotnet test` passing and `npm run build` passing. Record the time taken. The database is created automatically on first run, with no manual SQL step.
- **Release acceptance checks:** all seven must pass. For each one, the completion notes name the proving test(s) or manual step and the result:
  - AC-1, valid task title: automated integration tests.
  - AC-2, task must reference an existing project: automated integration tests.
  - AC-3, invalid status/priority rejected in body and filters: automated integration tests.
  - AC-4, task updates persist across a backend restart: the temp-file SQLite test, plus a manual restart check **repeated on the fresh clone**.
  - AC-5, project delete cascades to its tasks: automated integration tests.
  - AC-6, dashboard totals reflect saved data: automated integration tests.
  - AC-7, the app runs from the documented commands: the manual fresh-clone check.
- **Persistence:** all data lives in the SQLite file on disk and survives a backend restart.
- **Responsiveness:** with 500 tasks, the Tasks page, a filtered list and the Dashboard each load in **under 1 s**, measured in the browser's network panel. Create the 500 tasks with a throwaway `curl` loop against the API. Don't commit it and don't add a dependency.
- **Usability:** works in current Chrome and Firefox (Edge if available). Two user journeys must be completable **keyboard-only**:
  - Plan the week: create a project, add tasks, filter to High + Todo, set a task to InProgress, check the dashboard.
  - Clean up: delete a project, confirm the prompt that names its task count, and see the dashboard totals drop.

  Every input has a visible label.
- **API docs:** `/swagger` (Development only) lists every endpoint in the API contract:
  - `/api/health`
  - projects: GET list and by id, POST, PUT, DELETE
  - tasks: GET list (with filters) and by id, POST, PUT, DELETE
  - `/api/dashboard`
- **Definition of Done (both stories):**
  - `dotnet build`, `dotnet test` and `npm run build` pass. Actually run them; never claim a pass without running.
  - The behavior has been checked in the browser.
  - Committed with a clear message.
  - Only free, open-source tools are used.
- **Release bar:** no known bugs that lose data or crash the app.
- **Out of scope:** no new features, no frontend test framework, no CI, deployment, HTTPS or containers, and no seed/demo data committed to the repo. Adding features or tests only to raise coverage works against the goals.

## Technical Decisions

- **No new dependencies.** Story 5.2 must not add a UI library or kit. If a package ever seems needed, it has to be justified and recorded first.
- **Styling:** plain CSS in `frontend/src/index.css` only. Interactive elements stay native (`button`, `a`, `select`, `input`), and every input keeps its `<label htmlFor>`.
- **Display vs. wire values:** statuses may *display* as "To do", "In progress" and "Done". The values sent to and received from the API stay `Todo`, `InProgress` and `Done`. Select options and filters still come from the `TASK_STATUSES` / `TASK_PRIORITIES` arrays, and the filter URL parameters keep the API names.
- **Polish must not change behavior:** no changes to API calls, routes, validation or the backend.
- **Persistence test pattern (already in place):** the restart test writes through one API factory on a temp `.db` file, disposes it, calls `SqliteConnection.ClearAllPools()`, then reads back through a second factory on the same file. Integration tests otherwise use a shared-cache in-memory SQLite database per test, never the EF InMemory provider.
- **Local topology for manual checks:**
  - The API runs at `http://localhost:5080` (HTTP only, no CORS), and `taskflow.db` is created in the API project folder.
  - Vite runs at `5173` with `strictPort` and proxies `/api`.
  - To reset the data, stop the API and delete `taskflow.db*`.
- **Performance basis:** reads are no-tracking DTO projections, with no pagination and no extra indexes at this volume. If the 1 s target fails, investigate before adding indexes or pagination. Both are deferred by design.

## UX & Interaction Patterns

- Every interactive element needs a **visible focus outline**. Aim for consistent spacing and readable task and project tables.
- Keyboard flow:
  - Nav links, project and task title links, the filter selects, form inputs, and the Add, Edit and Delete buttons are all reachable and operable with Tab and Enter.
  - Deletes use the browser's `window.confirm`. The project prompt reads `Delete project "X" and its N tasks?`.
- Errors still show:
  - Each field error sits next to its input, and the user's typed values are kept.
  - Unmatched errors, an unreachable API or an unexpected error go to the error banner ("Cannot reach the server. Is the API running?"), never a blank screen.
- Empty-state wording stays fixed:
  - "No projects yet. Create your first project."
  - "No tasks yet."
  - "No tasks match these filters."
  - "Project not found."
  - "Task not found."

## Cross-Story Dependencies

- Story 5.1 depends on Epics 1–4 being complete: its checklist relies on their integration tests (validation, cascade, filters, dashboard, persistence). If a check fails, fix the cause; don't mark the check as passing.
- Story 5.2 happens only after 5.1, and only in leftover buffer time. It is cut first if the schedule slips. After polishing, re-run `dotnet test` and `npm run build`, and repeat the keyboard pass.
