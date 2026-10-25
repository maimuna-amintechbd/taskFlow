using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests.Dashboard;

/// <summary>
/// GET /api/dashboard. The AC-6 tests each read the totals, change data through the
/// API, and read them again, proving the totals reflect saved data on every request.
/// </summary>
public class DashboardTests : IDisposable
{
    private readonly TaskFlowApiFactory _factory;
    private readonly HttpClient _client;

    public DashboardTests()
    {
        _factory = new TaskFlowApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private async Task<DashboardDto> GetDashboardAsync()
    {
        var response = await _client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardDto>(ApiJson.Options);
        Assert.NotNull(dashboard);
        return dashboard;
    }

    /// <summary>
    /// Two projects holding 1 Todo, 3 InProgress and 2 Done: totals 6 / 2 / 4. The bucket
    /// sizes differ so a swapped or missing status shows up as a wrong number.
    /// The second project holds exactly two tasks: one InProgress and one Done.
    /// </summary>
    private async Task<(ProjectDto First, ProjectDto Second, TaskDto[] Tasks)> CreateMixedTasksAsync()
    {
        var first = await _client.CreateProjectAsync("First");
        var second = await _client.CreateProjectAsync("Second");
        TaskDto[] tasks =
        [
            await _client.CreateTaskAsync(first.Id, "Todo", status: "Todo"),
            await _client.CreateTaskAsync(first.Id, "InProgress 1", status: "InProgress"),
            await _client.CreateTaskAsync(first.Id, "InProgress 2", status: "InProgress"),
            await _client.CreateTaskAsync(first.Id, "Done 1", status: "Done"),
            await _client.CreateTaskAsync(second.Id, "InProgress 3", status: "InProgress"),
            await _client.CreateTaskAsync(second.Id, "Done 2", status: "Done"),
        ];
        return (first, second, tasks);
    }

    [Fact]
    public async Task Get_NoTasks_ReturnsAllZeros()
    {
        // A project without tasks must not count as anything.
        await _client.CreateProjectAsync("Empty");

        var dashboard = await GetDashboardAsync();

        Assert.Equal(new DashboardDto(Total: 0, Completed: 0, Pending: 0), dashboard);
    }

    [Fact]
    public async Task Get_NoTasks_SendsCamelCaseNumbers()
    {
        using var document = JsonDocument.Parse(await _client.GetStringAsync("/api/dashboard"));

        foreach (var name in new[] { "total", "completed", "pending" })
        {
            Assert.True(document.RootElement.TryGetProperty(name, out var value), $"Missing \"{name}\".");
            Assert.Equal(JsonValueKind.Number, value.ValueKind);
            Assert.Equal(0, value.GetInt32());
        }
    }

    [Fact]
    public async Task Get_MixedStatusesInTwoProjects_CountsAllProjects()
    {
        await CreateMixedTasksAsync();

        var dashboard = await GetDashboardAsync();

        Assert.Equal(new DashboardDto(Total: 6, Completed: 2, Pending: 4), dashboard);
    }

    [Fact]
    public async Task AC6_Get_AfterCreatingTodoTask_TotalAndPendingRiseByOne()
    {
        var (first, _, _) = await CreateMixedTasksAsync();
        var before = await GetDashboardAsync();

        await _client.CreateTaskAsync(first.Id, "New", status: "Todo");

        var after = await GetDashboardAsync();
        Assert.Equal(before with { Total = before.Total + 1, Pending = before.Pending + 1 }, after);
    }

    [Fact]
    public async Task AC6_Get_AfterEditingInProgressTaskToDone_MovesOneFromPendingToCompleted()
    {
        var (_, _, tasks) = await CreateMixedTasksAsync();
        var inProgress = tasks[1];
        var before = await GetDashboardAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/tasks/{inProgress.Id}",
            new SaveTaskRequest
            {
                Title = inProgress.Title,
                ProjectId = inProgress.ProjectId,
                Status = "Done",
                Priority = inProgress.Priority.ToString(),
            },
            ApiJson.Options);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var after = await GetDashboardAsync();
        Assert.Equal(before with { Completed = before.Completed + 1, Pending = before.Pending - 1 }, after);
    }

    [Fact]
    public async Task AC6_Get_AfterDeletingDoneTask_TotalAndCompletedDropByOne()
    {
        var (_, _, tasks) = await CreateMixedTasksAsync();
        var done = tasks[3];
        var before = await GetDashboardAsync();

        var response = await _client.DeleteAsync($"/api/tasks/{done.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var after = await GetDashboardAsync();
        Assert.Equal(before with { Total = before.Total - 1, Completed = before.Completed - 1 }, after);
    }

    [Fact]
    public async Task AC6_Get_AfterDeletingProjectWithTwoTasks_DropsThoseTasksFromTheCounts()
    {
        var (_, second, _) = await CreateMixedTasksAsync();
        var before = await GetDashboardAsync();
        Assert.Equal(new DashboardDto(Total: 6, Completed: 2, Pending: 4), before);

        var response = await _client.DeleteAsync($"/api/projects/{second.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // The cascade removed the project's InProgress and Done tasks; the first project's stay.
        var after = await GetDashboardAsync();
        Assert.Equal(
            before with { Total = before.Total - 2, Completed = before.Completed - 1, Pending = before.Pending - 1 },
            after);
    }
}
