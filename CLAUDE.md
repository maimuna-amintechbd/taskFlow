# TaskFlow — Project Instructions

## Goal
Build a beginner-friendly, portfolio-quality task management app.

## Stack
- Backend: C#, ASP.NET Core Web API (.NET 10)
- Data access: Entity Framework Core
- Database: SQLite
- Frontend: React, TypeScript, Vite
- API docs: OpenAPI/Swagger where supported
- Tests: xUnit for backend

## Architecture
- Keep frontend and backend in separate directories.
- Separate HTTP handling, business logic where needed, and data access.
- Use DTOs for API request and response models.
- Use asynchronous database operations.
- Avoid unnecessary abstractions.

## Conventions
- Name the task entity `TaskItem`, not `Task` (avoids clashing with
  `System.Threading.Tasks.Task`). API routes stay `/api/tasks`.
- Serialize enums (status, priority) as strings with `JsonStringEnumConverter`,
  and reject undefined enum values.
- Deleting a project cascades to its tasks; enforce it in the EF Core relationship.
- Integration tests use SQLite (in-memory connection), not the EF InMemory
  provider, so foreign keys and cascade deletes are actually enforced.

## Learning rules
- Explain unfamiliar C# and .NET concepts.
- Implement one small, testable story at a time.
- Do not skip tests or validation silently.
- Explain important design decisions.
- Do not add dependencies without explaining why.
- Run tests and builds after changes.
- Never claim a command passed unless it was actually run.

## Scope
Single-user MVP. No authentication, payments, microservices,
or real-time features in the first week.
