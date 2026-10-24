using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskFlow.Api.Tests.TestInfrastructure;

/// <summary>
/// Starts the real API in memory so tests can call it over HttpClient.
/// Each test creates its own factory, so tests never share state.
/// Story 2.1 adds the in-memory SQLite database here; Story 3.1 adds the fixed clock.
/// </summary>
public class TaskFlowApiFactory : WebApplicationFactory<Program>
{
}
