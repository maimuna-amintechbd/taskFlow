- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-walking-skeleton.md`
  summary: Add a repeatable check for `frontend/src/api/client.ts` error mapping (unreachable 502/fetch failure → status 0, JSON error → message=title, `$.Status` → `status`).
  evidence: Verification-gap review — only a one-off Node run and manual checks cover it; the MVP forbids a frontend test framework, so options are a dependency-free committed `node --test` script with stubbed `fetch`, or a manual checklist item in Story 5.1.
- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-walking-skeleton.md`
  summary: Verify the OpenAPI document shows `TaskItemStatus`/`TaskPriority` as string enums (converter is registered on MVC JsonOptions only).
  evidence: Unverified (maybe-false, would be medium for NFR-6). Settle in Story 3.1 by checking `/openapi/v1.json`; if ints appear, also register the converter via `ConfigureHttpJsonOptions`.
- source_spec: `_bmad-output/implementation-artifacts/spec-2-1-create-and-list-projects.md`
  summary: Automated checks for ProjectsPage behaviour (field errors next to inputs, unmatched keys to banner, form kept on error / cleared on success, empty state).
  evidence: Verification-gap review — only `npm run build` and manual/headless browser checks cover it; the MVP has no frontend test runner by architecture decision. Same root as the client.ts deferral from Story 1.1.
