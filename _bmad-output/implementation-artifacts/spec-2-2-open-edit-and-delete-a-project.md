---
title: 'Story 2.2 — Open, edit and delete a project'
type: 'feature'
created: '2026-10-24'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '5313d32e90f76a3155dd0fd1386fd09f149d48fc'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-2-context.md'
  - '{project-root}/CLAUDE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Projects can be created and listed, but not opened, corrected or removed, so the list can't stay accurate.

**Approach:** Add `PUT` and `DELETE /api/projects/{id}`, reusing Story 2.1's `ValidateAndApply` and `ToDto`. Add a project page at `/projects/:id` with details, an Edit mode (the pre-filled `ProjectForm`) and Delete with confirmation. Names in the projects list become links.

## Boundaries & Constraints

**Always:**
- Reuse `ProjectService.ValidateAndApply`, `ToDto`, `ServiceResult`, `ProjectForm`, `projectFormFields`, `ErrorBanner` and `request<T>`; extend them, don't duplicate them.
- PUT is a full replace. Look up the route id first: unknown → 404 before any validation.
- Delete removes only the project row; it never loads tasks (the cascade comes in Story 3.1).
- Add OpenAPI `ProducesResponseType` attributes for every new response (200/400 `ValidationProblemDetails`/404, 204/404).
- The page re-fetches after a save instead of patching local state. Native buttons and links only.

**Never:**
- No tasks, task list on the project page, or cascade (Epic 3). No new dependencies. No custom modal: use `window.confirm`.
- No DataAnnotations validation, repositories or HTML constraint attributes.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Update OK | `PUT {name:"  New  ", description:"  d  "}` | 200 `ProjectDto` with `name:"New"`, `description:"d"`, correct `taskCount` | N/A |
| Full replace | PUT without `description` | 200, `description:null` | N/A |
| Update invalid name | empty name / 101 chars | 400 with Story 2.1's exact messages; GET still shows the old values | nothing saved |
| Unknown id, valid body | `PUT /api/projects/9999` | 404 ProblemDetails | N/A |
| Unknown id, invalid body | `PUT /api/projects/9999 {name:""}` | 404, not 400 | N/A |
| Delete | `DELETE /api/projects/{id}` | 204; then GET → 404 and the list no longer contains it | N/A |
| Delete unknown | `DELETE /api/projects/9999` | 404 ProblemDetails | N/A |
| Open from list | click or Enter on a project name | `/projects/:id` shows name, description (or nothing when null), "N tasks", Edit, Delete | load failure → `ErrorBanner` |
| Edit invalid | clear the name, Save | "Name is required." by the input; typed values kept; still in edit mode | unmatched keys → banner |
| Edit OK / Cancel | Save valid / Cancel | back to details, re-fetched values / unchanged values | N/A |
| Delete confirm | Delete → OK / Cancel | confirm text `Delete project "X" and its N tasks?` ("1 task" when N = 1); OK → `/projects` without it; Cancel → nothing | failure → banner |
| Unknown project page | `/projects/9999` or `/projects/abc` | `NotFoundPage` with "Project not found." | N/A |

</frozen-after-approval>

## Code Map

- `backend/TaskFlow.Api/Services/ProjectService.cs` -- has `ListAsync`, `GetByIdAsync`, `CreateAsync`, private `FindDtoAsync`, private static `ValidateAndApply`, `ToDto`; add `UpdateAsync`, `DeleteAsync`.
- `backend/TaskFlow.Api/Controllers/ProjectsController.cs` -- `List`, `GetById`, `Create` with an inline `switch`; add `Update`, `Delete` in the same style (no `Async` suffix).
- `backend/TaskFlow.Api.Tests/Projects/` -- the existing pattern: a factory per test, `ApiJson.Options`, `CreateProjectAsync`.
- `frontend/src/components/ProjectForm.tsx` -- form state starts empty and clears on success; add optional initial values and an optional Cancel.
- `frontend/src/pages/ProjectsPage.tsx` -- the list renders `<span className="project-name">`; make it a `Link`.
- `frontend/src/pages/NotFoundPage.tsx` -- fixed "Page not found"; add an optional `message` prop (default unchanged).
- `frontend/src/App.tsx` -- add the `/projects/:id` route; `frontend/src/api/projects.ts` -- add `getProject`, `updateProject`, `deleteProject`.

## Tasks & Acceptance

**Execution:**
- [x] `backend/TaskFlow.Api/Services/ProjectService.cs` -- `UpdateAsync(id, request)`: find the tracked project (null → NotFound), `ValidateAndApply`, save, re-read via `FindDtoAsync`. `DeleteAsync(id)`: find (null → NotFound), `Remove`, save, `ServiceResult.Ok()`.
- [x] `backend/TaskFlow.Api/Controllers/ProjectsController.cs` -- `[HttpPut("{id:int}")] Update` → Ok/NotFound/ValidationProblem; `[HttpDelete("{id:int}")] Delete` → NoContent/NotFound; with `ProducesResponseType` attributes.
- [x] `backend/TaskFlow.Api.Tests/Projects/UpdateProjectTests.cs`, `DeleteProjectTests.cs` -- one test per API matrix row (Update OK through Delete unknown).
- [x] `frontend/src/api/projects.ts` -- `getProject(id)`, `updateProject(id, body)`, `deleteProject(id)` (`request<void>`).
- [x] `frontend/src/components/ProjectForm.tsx` -- optional `initialValues` (seed the state) and `onCancel` (renders a Cancel button); clear-on-success stays the default behaviour for create.
- [x] `frontend/src/pages/NotFoundPage.tsx` -- optional `message` prop shown as the heading.
- [x] `frontend/src/pages/ProjectPage.tsx` -- states loading / loaded / not found (ApiError 404 → `<NotFoundPage message="Project not found." />`) / error; details view, edit mode, delete flow, using `PROJECT_FORM_FIELDS` for the error split.
- [x] `frontend/src/App.tsx`, `pages/ProjectsPage.tsx` -- add the route; project names become `<Link to={`/projects/${id}`}>`.
- [x] `frontend/src/index.css` -- small styles for the details view and the button row; secondary buttons must not look like the primary Create button.

**Acceptance Criteria:**
- Given I edit a project, restart the API and reload, then the change is still there (manual).
- Given `ProjectsController`, when I read it, then `Update`/`Delete` hold no rules and no DbContext.

## Implementation Notes

- Review fixes: `UpdateAsync` comment narrowed (framework 400s unreadable bodies first); `ProjectForm` clears on success only in create mode (no `initialValues`); Edit disabled while deleting; `OpenApi_UpdateAndDeleteProject_DocumentResponses` added.
- Verification (2026-10-24): `dotnet build` 0 warnings/0 errors; `dotnet test` 29/29; `npm run build` ok; `npm run lint` clean. Headless Chrome via DevTools protocol on a fresh DB: Enter on a focused project link opens `/projects/2`; details show name, "0 tasks", Edit, Delete; Edit is pre-filled; empty name → "Name is required.", stays in edit mode, values kept; Cancel → unchanged details; Save "  Portfolio website  "/"  Built with React  " → trimmed values shown. API restarted → edited values still listed. Delete confirm text `Delete project "Portfolio website" and its 0 tasks?`; Cancel stays, OK → `/projects` without it. `/projects/9999` and `/projects/abc` → "Project not found."

## Spec Change Log

## Review Triage Log

| # | Layer | Finding | Verdict | Evidence / route |
|---|---|---|---|---|
| B1 / E4 | blind, edge | "404 wins over 400" comment overstated: `[ApiController]` 400s unreadable bodies before the action | low | Matrix row (`{name:""}` → 404) holds and is tested; only the comment overclaims. **patch** (comment). |
| B5 | blind | `ProjectForm` resets to pre-edit `initialValues` on success; relies on parent unmounting; setState after unmount | medium | Any future page keeping the form mounted would show old values after a save. One-condition fix → **patch**. |
| B8 / E8 | blind, edge | Edit clickable while DELETE in flight | low | Direct `disabled={deleting}` → **patch**. |
| VG1 | verify | OpenAPI responses for PUT/DELETE unasserted | medium | Pre-verified gap. **patch**: OpenAPI test. |
| VG2 | verify | No automated frontend check for ProjectForm/ProjectPage changes | medium | No FE test runner by architecture decision → **defer**. |
| B2 | blind | Update tests don't cover description rules / both-errors on PUT | low | Same `ValidateAndApply` method already covered by create tests; spec required name cases on update (done). Rejected. |
| B3 / E7 | blind, edge | Failed re-fetch after a successful save shows error/not-found | low | Needs a GET failure or a second tab deleting in between; single local user. Rejected. |
| B3b / E9 | blind, edge | `loadProject` lacks the ignore guard | low | React ignores late setState; same rejection as Story 2.1 B10. Rejected. |
| B4 / E5 / E6 | blind, edge | PUT/DELETE 404 (deleted in another tab) shows generic banner instead of not-found | low | Requires two tabs; PRD accepts last-save-wins across tabs; fix adds branches. Rejected. |
| B6 | blind | Remaining duplication: catch block, FALLBACK_MESSAGE, task/tasks label | low | Field-split already shared; leftovers are a few lines in two pages. Rejected. |
| B7 | blind | No focus management on Edit/Cancel/Save | low | Basic keyboard access works; polish is Story 5.2 (same as 2.1 B11). Rejected. |
| B9 | blind | Project not-found page links to dashboard, punctuation differs | low | Wording fixed by AD-12 conventions; link still useful. Rejected. |
| B10 | blind | Global `button` is primary; no hover styles | low | Deliberate: base = primary, `.button-secondary`/`.button-danger` opt out; hover is polish (5.2). Rejected. |
| B11 / E1 / E2 / E3 | blind, edge | Row deleted between FindAsync and SaveChanges/re-read → 500 | low | Needs concurrent requests; single user (same as 2.1 E3). Rejected. |
| B12 | blind | Verification results not recorded | false | Recorded in Implementation Notes at step 5; commands were run (28/28, build, lint). |

## Verification

**Commands:**
- `cd backend && dotnet build` -- expected: 0 errors, 0 warnings
- `cd backend && dotnet test` -- expected: all pass (20 existing + new)
- `cd frontend && npm run build && npm run lint` -- expected: success, no warnings

**Manual checks:**
- Browser (headless Chrome via the DevTools protocol is acceptable): list → click a name → details; Edit with an empty name → message, values kept; valid Save → updated; Delete → OK → back at `/projects` without it; `/projects/9999` → "Project not found."
