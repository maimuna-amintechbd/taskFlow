using System.Net;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Data;
using TaskFlow.Api.Models;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests.Projects;

/// <summary>
/// Proves the database itself enforces the Project–TaskItem relationship. This is
/// why tests use real SQLite: the EF InMemory provider enforces neither.
/// </summary>
public class CascadeAndForeignKeyTests : IDisposable
{
    private readonly TaskFlowApiFactory _factory;
    private readonly HttpClient _client;

    public CascadeAndForeignKeyTests()
    {
        _factory = new TaskFlowApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task DeleteProject_WithTasks_DatabaseRemovesItsTasks()
    {
        var doomed = await _client.CreateProjectAsync("Doomed");
        var kept = await _client.CreateProjectAsync("Kept");
        await _client.CreateTaskAsync(doomed.Id, "One");
        await _client.CreateTaskAsync(doomed.Id, "Two");
        var keptTask = await _client.CreateTaskAsync(kept.Id, "Stays");

        var response = await _client.DeleteAsync($"/api/projects/{doomed.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
        Assert.Equal(0, await db.TaskItems.CountAsync(t => t.ProjectId == doomed.Id));
        Assert.Equal([keptTask.Id], await db.TaskItems.Select(t => t.Id).ToListAsync());
    }

    [Fact]
    public async Task InsertTask_WithUnknownProjectId_ThrowsDbUpdateException()
    {
        // The factory already started the host when the client was created, so
        // Program.cs has applied the migrations; no request is needed first.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
        db.TaskItems.Add(new TaskItem
        {
            Title = "Orphan",
            ProjectId = 9999,
            CreatedAt = _factory.Clock.UtcNow.UtcDateTime,
        });

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        // Make sure SQLite's foreign key constraint is the reason (error 19 = SQLITE_CONSTRAINT).
        var sqlite = Assert.IsType<SqliteException>(exception.InnerException);
        Assert.Equal(19, sqlite.SqliteErrorCode);
        Assert.Contains("FOREIGN KEY", sqlite.Message);
    }
}
