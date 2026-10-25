using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests.Tasks;

public class ListTasksTests : IDisposable
{
    private readonly TaskFlowApiFactory _factory;
    private readonly HttpClient _client;

    public ListTasksTests()
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

    private async Task AssertProjectIdErrorAsync(string url, string expectedMessage)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiJson.Options);
        Assert.NotNull(problem);
        var (key, messages) = Assert.Single(problem.Errors);
        Assert.Equal("projectId", key);
        Assert.Equal([expectedMessage], messages);
    }

    [Fact]
    public async Task List_NoTasks_ReturnsEmptyList()
    {
        var tasks = await GetListAsync("/api/tasks");

        Assert.Empty(tasks);
    }

    [Fact]
    public async Task List_TasksCreatedOverTime_NewestFirst()
    {
        var project = await _client.CreateProjectAsync("P");
        // Ids increase with creation too, so the ids alone can't tell the two orders
        // apart. Moving the clock *backwards* makes CreatedAt and id disagree.
        var start = _factory.Clock.UtcNow;
        var middle = await _client.CreateTaskAsync(project.Id, "Middle");
        _factory.Clock.UtcNow = start.AddMinutes(5);
        var newest = await _client.CreateTaskAsync(project.Id, "Newest");
        _factory.Clock.UtcNow = start.AddMinutes(-5);
        var oldest = await _client.CreateTaskAsync(project.Id, "Oldest");

        var tasks = await GetListAsync("/api/tasks");

        Assert.Equal([newest, middle, oldest], tasks);
    }

    [Fact]
    public async Task List_SameCreatedAt_OrderedByIdDescending()
    {
        var project = await _client.CreateProjectAsync("P");
        var first = await _client.CreateTaskAsync(project.Id, "First");
        var second = await _client.CreateTaskAsync(project.Id, "Second");
        Assert.Equal(first.CreatedAt, second.CreatedAt);

        var tasks = await GetListAsync("/api/tasks");

        Assert.Equal([second.Id, first.Id], tasks.Select(t => t.Id).ToArray());
    }

    [Fact]
    public async Task List_ProjectFilter_ReturnsOnlyThatProjectsTasksNewestFirst()
    {
        var project = await _client.CreateProjectAsync("P");
        var other = await _client.CreateProjectAsync("Other");
        var start = _factory.Clock.UtcNow;
        var older = await _client.CreateTaskAsync(project.Id, "Older");
        _factory.Clock.UtcNow = start.AddMinutes(1);
        await _client.CreateTaskAsync(other.Id, "Not mine");
        _factory.Clock.UtcNow = start.AddMinutes(2);
        var newer = await _client.CreateTaskAsync(project.Id, "Newer");

        var tasks = await GetListAsync($"/api/tasks?projectId={project.Id}");

        Assert.Equal([newer, older], tasks);
    }

    [Fact]
    public async Task List_ExistingProjectWithNoTasks_ReturnsEmptyList()
    {
        var project = await _client.CreateProjectAsync("Empty");
        var other = await _client.CreateProjectAsync("Other");
        await _client.CreateTaskAsync(other.Id, "Not mine");

        var tasks = await GetListAsync($"/api/tasks?projectId={project.Id}");

        Assert.Empty(tasks);
    }

    [Fact]
    public async Task List_UnknownProject_Returns400UnderProjectId()
    {
        await AssertProjectIdErrorAsync("/api/tasks?projectId=9999", "Project 9999 does not exist.");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1.5")]
    [InlineData("%201")] // " 1": surrounding whitespace is not accepted
    public async Task List_ProjectIdNotAWholeNumber_Returns400UnderProjectId(string projectId)
    {
        await AssertProjectIdErrorAsync($"/api/tasks?projectId={projectId}", "Project id must be a whole number.");
    }
}
