using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests.Projects;

public class DeleteProjectTests : IDisposable
{
    private readonly TaskFlowApiFactory _factory;
    private readonly HttpClient _client;

    public DeleteProjectTests()
    {
        _factory = new TaskFlowApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Delete_ExistingProject_Returns204ThenGetReturns404()
    {
        var doomed = await _client.CreateProjectAsync("Doomed");
        var kept = await _client.CreateProjectAsync("Kept");

        var response = await _client.DeleteAsync($"/api/projects/{doomed.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var get = await _client.GetAsync($"/api/projects/{doomed.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        var projects = await _client.GetFromJsonAsync<List<ProjectDto>>("/api/projects", ApiJson.Options);
        Assert.NotNull(projects);
        Assert.Equal([kept.Id], projects.Select(p => p.Id).ToArray());
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404ProblemDetails()
    {
        var response = await _client.DeleteAsync("/api/projects/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJson.Options);
        Assert.Equal(404, problem?.Status);
    }
}
