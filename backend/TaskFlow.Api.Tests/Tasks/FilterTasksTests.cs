using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests.Tasks;

/// <summary>
/// GET /api/tasks?status=&amp;priority= (Story 4.1), including AC-3's query-string half:
/// invalid filter values are a 400, never silently ignored.
/// </summary>
public class FilterTasksTests : IDisposable
{
    private const string StatusMessage = "Status must be one of: Todo, InProgress, Done.";
    private const string PriorityMessage = "Priority must be one of: Low, Medium, High.";

    private readonly TaskFlowApiFactory _factory;
    private readonly HttpClient _client;

    public FilterTasksTests()
    {
        _factory = new TaskFlowApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private async Task<List<TaskDto>> GetListAsync(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tasks = await response.Content.ReadFromJsonAsync<List<TaskDto>>(ApiJson.Options);
        Assert.NotNull(tasks);
        return tasks;
    }

    private async Task<IDictionary<string, string[]>> GetErrorsAsync(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiJson.Options);
        Assert.NotNull(problem);
        return problem.Errors;
    }

    /// <summary>
    /// Two projects, every status/priority mix that the tests need, each task one
    /// minute newer than the previous so "newest first" is visible in the result.
    /// </summary>
    private async Task<Seed> SeedAsync()
    {
        var p = await _client.CreateProjectAsync("P");
        var q = await _client.CreateProjectAsync("Q");
        var start = _factory.Clock.UtcNow;
        var minute = 0;

        async Task<TaskDto> Create(int projectId, string title, string status, string priority)
        {
            _factory.Clock.UtcNow = start.AddMinutes(minute++);
            return await _client.CreateTaskAsync(projectId, title, status, priority);
        }

        return new Seed(
            P: p,
            PTodoHigh: await Create(p.Id, "P todo high", "Todo", "High"),
            QTodoHigh: await Create(q.Id, "Q todo high", "Todo", "High"),
            PTodoLow: await Create(p.Id, "P todo low", "Todo", "Low"),
            PDoneHigh: await Create(p.Id, "P done high", "Done", "High"),
            QDoneLow: await Create(q.Id, "Q done low", "Done", "Low"),
            PInProgressMedium: await Create(p.Id, "P in progress medium", "InProgress", "Medium"),
            QInProgressHigh: await Create(q.Id, "Q in progress high", "InProgress", "High"));
    }

    private record Seed(
        ProjectDto P,
        TaskDto PTodoHigh,
        TaskDto QTodoHigh,
        TaskDto PTodoLow,
        TaskDto PDoneHigh,
        TaskDto QDoneLow,
        TaskDto PInProgressMedium,
        TaskDto QInProgressHigh);

    [Fact]
    public async Task List_StatusAndPriorityAnyCase_ReturnsOnlyTasksMatchingBothNewestFirst()
    {
        var seed = await SeedAsync();

        var tasks = await GetListAsync("/api/tasks?status=todo&priority=HIGH");

        Assert.Equal([seed.QTodoHigh, seed.PTodoHigh], tasks);
    }

    [Fact]
    public async Task List_StatusOnly_ReturnsOnlyThatStatus()
    {
        var seed = await SeedAsync();

        var tasks = await GetListAsync("/api/tasks?status=Done");

        Assert.Equal([seed.QDoneLow, seed.PDoneHigh], tasks);
    }

    [Fact]
    public async Task List_PriorityOnly_ReturnsOnlyThatPriority()
    {
        var seed = await SeedAsync();

        var tasks = await GetListAsync("/api/tasks?priority=Low");

        Assert.Equal([seed.QDoneLow, seed.PTodoLow], tasks);
    }

    [Fact]
    public async Task List_StatusWithProject_ReturnsOnlyThatProjectsMatches()
    {
        var seed = await SeedAsync();

        var tasks = await GetListAsync($"/api/tasks?projectId={seed.P.Id}&status=InProgress");

        Assert.Equal([seed.PInProgressMedium], tasks);
    }

    [Fact]
    public async Task List_NoFilters_ReturnsAllTasksNewestFirst()
    {
        var seed = await SeedAsync();

        var tasks = await GetListAsync("/api/tasks");

        Assert.Equal(
            [
                seed.QInProgressHigh, seed.PInProgressMedium, seed.QDoneLow, seed.PDoneHigh,
                seed.PTodoLow, seed.QTodoHigh, seed.PTodoHigh,
            ],
            tasks);
    }

    [Fact]
    public async Task List_ValidFiltersMatchingNothing_ReturnsEmptyList()
    {
        await SeedAsync();

        var tasks = await GetListAsync("/api/tasks?status=InProgress&priority=Low");

        Assert.Empty(tasks);
    }

    [Theory]
    [InlineData("status=7", "status", StatusMessage)] // undefined number
    [InlineData("status=2", "status", StatusMessage)] // defined number (Done) is still not a name
    [InlineData("status=", "status", StatusMessage)] // empty is not "All"
    [InlineData("status=Hgh", "status", StatusMessage)] // a priority typo sent as a status
    [InlineData("status=%20Todo", "status", StatusMessage)] // surrounding whitespace
    [InlineData("priority=7", "priority", PriorityMessage)]
    [InlineData("priority=", "priority", PriorityMessage)]
    [InlineData("priority=2", "priority", PriorityMessage)]
    [InlineData("priority=%20High", "priority", PriorityMessage)]
    [InlineData("priority=Hgh", "priority", PriorityMessage)]
    public async Task List_InvalidFilterValue_Returns400UnderThatKey(
        string queryString, string expectedKey, string expectedMessage)
    {
        await _client.CreateTaskAsync((await _client.CreateProjectAsync("P")).Id);

        var errors = await GetErrorsAsync($"/api/tasks?{queryString}");

        var (key, messages) = Assert.Single(errors);
        Assert.Equal(expectedKey, key);
        Assert.Equal([expectedMessage], messages);
    }

    [Fact]
    public async Task List_SeveralInvalidParameters_ReportsAllInOne400()
    {
        var errors = await GetErrorsAsync("/api/tasks?status=7&priority=7&projectId=abc");

        Assert.Equal(3, errors.Count);
        Assert.Equal([StatusMessage], errors["status"]);
        Assert.Equal([PriorityMessage], errors["priority"]);
        Assert.Equal(["Project id must be a whole number."], errors["projectId"]);
    }

    [Fact]
    public async Task List_ValidFiltersWithUnknownProject_Returns400UnderProjectIdOnly()
    {
        var errors = await GetErrorsAsync("/api/tasks?projectId=9999&status=Todo&priority=High");

        var (key, messages) = Assert.Single(errors);
        Assert.Equal("projectId", key);
        Assert.Equal(["Project 9999 does not exist."], messages);
    }

    [Fact]
    public async Task List_UnknownProjectAndInvalidFilters_ReportsAllInOne400()
    {
        var errors = await GetErrorsAsync("/api/tasks?projectId=9999&status=7&priority=7");

        Assert.Equal(3, errors.Count);
        Assert.Equal([StatusMessage], errors["status"]);
        Assert.Equal([PriorityMessage], errors["priority"]);
        Assert.Equal(["Project 9999 does not exist."], errors["projectId"]);
    }
}
