using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace TaskFlow.Api.Tests.TestInfrastructure;

/// <summary>
/// Starts the real API in memory so tests can call it over HttpClient.
/// Each test creates its own factory, so tests never share state.
/// Story 3.1 adds the fixed clock.
/// </summary>
public class TaskFlowApiFactory : WebApplicationFactory<Program>
{
    // A uniquely named, shared-cache in-memory SQLite database: every connection that
    // uses this exact string sees the same database, and no other factory can.
    private readonly string _connectionString =
        $"Data Source=taskflow-test-{Guid.NewGuid()};Mode=Memory;Cache=Shared";

    // An in-memory SQLite database is deleted when its last connection closes.
    // Keeping this one open for the factory's lifetime keeps the data alive between
    // requests, which each open and close their own connection.
    private readonly SqliteConnection _keepAliveConnection;

    public TaskFlowApiFactory()
    {
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Overrides ConnectionStrings:TaskFlow from appsettings.json. Program.cs then
        // runs the real migrations against this database at startup.
        builder.UseSetting("ConnectionStrings:TaskFlow", _connectionString);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAliveConnection.Dispose();
        }
    }
}
