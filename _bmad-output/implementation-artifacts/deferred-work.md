- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-walking-skeleton.md`
  summary: Add a repeatable check for `frontend/src/api/client.ts` error mapping (unreachable 502/fetch failure → status 0, JSON error → message=title, `$.Status` → `status`).
  evidence: Verification-gap review — only a one-off Node run and manual checks cover it; the MVP forbids a frontend test framework, so options are a dependency-free committed `node --test` script with stubbed `fetch`, or a manual checklist item in Story 5.1.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-walking-skeleton.md`
  summary: Verify the OpenAPI document shows `TaskItemStatus`/`TaskPriority` as string enums (converter is registered on MVC JsonOptions only).
  evidence: Unverified (maybe-false, would be medium for NFR-6). Settle in Story 3.1 by checking `/openapi/v1.json`; if ints appear, also register the converter via `ConfigureHttpJsonOptions`.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-1-create-and-list-projects.md`
  summary: Automated checks for ProjectsPage behaviour (field errors next to inputs, unmatched keys to banner, form kept on error / cleared on success, empty state).
  evidence: Verification-gap review — only `npm run build` and manual/headless browser checks cover it; the MVP has no frontend test runner by architecture decision. Same root as the client.ts deferral from Story 1.1.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-2-open-edit-and-delete-a-project.md`
  summary: Automated checks for ProjectPage and ProjectForm (edit keeps values on 400, create clears on success, 404 → "Project not found.", delete confirm wording).
  evidence: Verification-gap review — only build/lint and scripted headless-browser runs cover it; no frontend test runner in the MVP. Same root as the Story 1.1 and 2.1 deferrals; worth one decision for all three.
- source_spec: `_bmad-output/implementation-artifacts/spec-3-1-add-a-task-api-and-rules.md`
  summary: RESOLVED — the "OpenAPI shows TaskItemStatus/TaskPriority as string enums" item from Story 1.1 was a real bug, fixed in Story 3.1 (ConfigureHttpJsonOptions) and covered by OpenApiTests.
  evidence: `/openapi/v1.json` listed both enums as integers before the fix; now lists the names.
- source_spec: `_bmad-output/implementation-artifacts/spec-3-2-add-tasks-from-the-ui-and-list-them.md`
  summary: Automated checks for the task UI (TaskFormPage preselection rules, splitTaskFieldErrors, project rename → task rows show the new name).
  evidence: Verification-gap review; no frontend test runner in the MVP. Same root as the Story 1.1/2.1/2.2 entries — one decision for all four. Add "rename a project → task rows update" to the Story 5.1 manual checklist.
- source_spec: `_bmad-output/implementation-artifacts/spec-3-3-edit-a-task.md`
  summary: Automated checks for TaskEditPage/TaskForm edit mode (pre-filled values, '' → null and numeric projectId in the PUT body, "Task not found.", navigation to the saved project).
  evidence: Verification-gap review; no frontend test runner in the MVP. Same decision as the Story 1.1–3.2 entries.
