using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests.Tasks;

public class DeleteTaskTests : IDisposable
{
    private readonly TaskFlowApiFactory _factory;
    private readonly HttpClient _client;

    public DeleteTaskTests()
    {
        _factory = new TaskFlowApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Delete_ExistingTask_Returns204ThenGetReturns404AndTaskCountDrops()
    {
        var project = await _client.CreateProjectAsync("P");
        var doomed = await _client.CreateTaskAsync(project.Id, "Doomed");
        var kept = await _client.CreateTaskAsync(project.Id, "Kept");
        var before = await _client.GetFromJsonAsync<ProjectDto>($"/api/projects/{project.Id}", ApiJson.Options);
        Assert.Equal(2, before?.TaskCount);

        var response = await _client.DeleteAsync($"/api/tasks/{doomed.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var get = await _client.GetAsync($"/api/tasks/{doomed.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);

        var tasks = await _client.GetFromJsonAsync<List<TaskDto>>("/api/tasks", ApiJson.Options);
        Assert.NotNull(tasks);
        Assert.Equal([kept.Id], tasks.Select(t => t.Id).ToArray());

        var after = await _client.GetFromJsonAsync<ProjectDto>($"/api/projects/{project.Id}", ApiJson.Options);
        Assert.Equal(before! with { TaskCount = 1 }, after);
    }

    [Fact]
    public async Task Delete_SameIdTwice_Returns204Then404()
    {
        var project = await _client.CreateProjectAsync("P");
        var task = await _client.CreateTaskAsync(project.Id, "Once");

        var first = await _client.DeleteAsync($"/api/tasks/{task.Id}");
        var second = await _client.DeleteAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        Assert.Equal("application/problem+json", second.Content.Headers.ContentType?.MediaType);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>(ApiJson.Options);
        Assert.Equal(404, problem?.Status);
    }

    [Fact]
    public async Task Delete_LastTaskOfProject_ProjectRemainsWithZeroTasks()
    {
        var project = await _client.CreateProjectAsync("P");
        var task = await _client.CreateTaskAsync(project.Id, "Only");

        var response = await _client.DeleteAsync($"/api/tasks/{task.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var get = await _client.GetAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var after = await get.Content.ReadFromJsonAsync<ProjectDto>(ApiJson.Options);
        Assert.Equal(0, after?.TaskCount);
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404ProblemDetails()
    {
        var response = await _client.DeleteAsync("/api/tasks/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJson.Options);
        Assert.Equal(404, problem?.Status);
    }
}
