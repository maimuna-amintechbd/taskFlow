using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace TaskFlow.Api.Tests.TestInfrastructure;

/// <summary>
/// Starts the real API in memory so tests can call it over HttpClient.
/// Each test creates its own factory, so tests never share state.
/// The API's clock is replaced by <see cref="Clock"/>, which tests control.
/// </summary>
public class TaskFlowApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    // An in-memory SQLite database is deleted when its last connection closes.
    // Keeping this one open for the factory's lifetime keeps the data alive between
    // requests, which each open and close their own connection. Null for a file
    // database, which keeps its data on disk without any open connection.
    private readonly SqliteConnection? _keepAliveConnection;

    /// <param name="connectionString">
    /// Omit it (the usual case) for a private in-memory database. Pass a file database
    /// (e.g. "Data Source=/tmp/x.db") to test that data outlives the factory; the
    /// caller then owns the file and deletes it.
    /// </param>
    public TaskFlowApiFactory(string? connectionString = null)
    {
        if (connectionString is not null)
        {
            _connectionString = connectionString;
            return;
        }

        // A uniquely named, shared-cache in-memory SQLite database: every connection that
        // uses this exact string sees the same database, and no other factory can.
        _connectionString = $"Data Source=taskflow-test-{Guid.NewGuid()};Mode=Memory;Cache=Shared";
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();
    }

    /// <summary>The clock the API uses for "now" and "today". Settable per test.</summary>
    public FixedTimeProvider Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Overrides ConnectionStrings:TaskFlow from appsettings.json. Program.cs then
        // runs the real migrations against this database at startup.
        builder.UseSetting("ConnectionStrings:TaskFlow", _connectionString);

        // Runs after Program.cs registered its services, so this replaces
        // TimeProvider.System with the test clock.
        builder.ConfigureTestServices(services =>
        {
            // Without this check, the replacement below would hide a missing
            // registration in Program.cs and the tests would still pass.
            if (!services.Any(d => d.ServiceType == typeof(TimeProvider)))
            {
                throw new InvalidOperationException("Program.cs must register a TimeProvider.");
            }

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAliveConnection?.Dispose();
        }
    }
}
