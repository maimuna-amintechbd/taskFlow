# TaskFlow PRD — Addendum (technical notes for architecture)

Material here supports the PRD but belongs to the architecture step. The CLAUDE.md conventions below are binding. Everything else is a starting point for the architecture step.

## Stack (fixed by constraints)
- Backend: C#, ASP.NET Core Web API on .NET 10; EF Core with the SQLite provider; xUnit tests.
- Frontend: React + TypeScript + Vite.
- API docs: OpenAPI/Swagger in Development.

## Conventions carried from CLAUDE.md
- The task entity is named `TaskItem`, so it does not clash with `System.Threading.Tasks.Task`. Routes stay `/api/tasks`.
- `Status` and `Priority` are serialized as strings (`JsonStringEnumConverter` with `allowIntegerValues: false`). Names are matched case-insensitively; numbers and undefined names are rejected. Query-string filters need the same check, because default model binding accepts numbers such as `?status=7`. This covers AC-3.
- Project → TaskItem relationship: the foreign key is required and set to cascade on delete in the EF Core configuration. This covers AC-2 and AC-5.
- Integration tests use SQLite in-memory (keeping one connection open per test), not the EF InMemory provider.
- API request and response models are DTOs. Database access is async throughout.

## Likely API surface (to confirm in architecture)
- `GET/POST /api/projects`, `GET/PUT/DELETE /api/projects/{id}`
- `GET/POST /api/tasks` (GET accepts `status`, `priority` and `projectId` query parameters), `GET/PUT/DELETE /api/tasks/{id}`
- `GET /api/dashboard` → `{ total, completed, pending }`
- Errors: 400 as ASP.NET Core `ValidationProblemDetails` (field → messages). 404 for an unknown id in the route. An unknown `projectId` in a request body or filter is a 400.
- PUT replaces the whole task (FR-7). There are no defaults on edit.

## Due-date rule on edit (FR-7)
On PUT, compare the incoming due date with the stored value. If unchanged, accept it. If changed and before today, reject it. If null, clear it. Use `DateOnly` to avoid time-of-day bugs. Read today's date through an injectable clock (.NET's `TimeProvider`), so tests can fix the date and not break near midnight.

## Local run (AC-7) — expected README commands
- Prerequisites: .NET 10 SDK, Node.js LTS.
- Backend: `dotnet run` in the API project. The SQLite file is created or migrated automatically on startup. Whether to use migrations or `EnsureCreated` is an architecture decision; migrations are preferred so the schema can change later.
- Frontend: `npm install` then `npm run dev`.
- Tests: `dotnet test` from the backend folder.
- AC-4 (persistence across restarts) cannot be proven with in-memory SQLite. Verify it manually, or with one test that uses a temporary SQLite file.
- Settle the CORS origin (the Vite dev server) and the ports during architecture.
