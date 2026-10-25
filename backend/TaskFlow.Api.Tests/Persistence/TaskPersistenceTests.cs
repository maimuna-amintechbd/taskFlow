using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Models;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests.Persistence;

/// <summary>
/// AC-4: an edit survives a restart. Two factories (two separate API "processes")
/// share one temporary SQLite file; never the real taskflow.db.
/// </summary>
public class TaskPersistenceTests
{
    [Fact]
    public async Task UpdatedTask_AfterRestart_IsStillThere()
    {
        var path = Path.Combine(Path.GetTempPath(), $"taskflow-persistence-{Guid.NewGuid()}.db");
        var connectionString = $"Data Source={path}";
        try
        {
            int taskId;
            DateOnly dueDate;

            // First run: create a project and a task, then edit the task.
            using (var first = new TaskFlowApiFactory(connectionString))
            {
                var client = first.CreateClient();
                var project = await client.CreateProjectAsync("Persisted");
                var task = await client.CreateTaskAsync(project.Id, title: "Before restart");
                taskId = task.Id;
                dueDate = first.Clock.Today.AddDays(7);

                var response = await client.PutAsJsonAsync(
                    $"/api/tasks/{taskId}",
                    new
                    {
                        title = "After restart",
                        projectId = project.Id,
                        status = "InProgress",
                        priority = "High",
                        dueDate,
                    },
                    ApiJson.Options);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            // Release any pooled connection to the file before the second run opens it.
            SqliteConnection.ClearAllPools();
            Assert.True(File.Exists(path));

            // Second run: a brand-new API on the same file reads the edit back.
            using (var second = new TaskFlowApiFactory(connectionString))
            {
                var client = second.CreateClient();
                var saved = await client.GetFromJsonAsync<TaskDto>($"/api/tasks/{taskId}", ApiJson.Options);

                Assert.NotNull(saved);
                Assert.Equal("After restart", saved.Title);
                Assert.Equal(TaskItemStatus.InProgress, saved.Status);
                Assert.Equal(TaskPriority.High, saved.Priority);
                Assert.Equal(dueDate, saved.DueDate);
                Assert.Equal("Persisted", saved.ProjectName);
            }
        }
        finally
        {
            // Microsoft.Data.Sqlite pools connections, and a pooled connection keeps the
            // file open (Windows would refuse the delete). Clearing the pools closes them.
            SqliteConnection.ClearAllPools();
            // Best effort: a failed delete must not hide the real assertion failure above.
            try
            {
                File.Delete(path);
                // SQLite's side files, in case a journal mode left any (File.Delete ignores missing files).
                File.Delete(path + "-wal");
                File.Delete(path + "-shm");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        Assert.False(File.Exists(path));
    }
}
