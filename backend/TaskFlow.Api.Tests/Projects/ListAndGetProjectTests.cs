using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests.Projects;

public class ListAndGetProjectTests : IDisposable
{
    private readonly TaskFlowApiFactory _factory;
    private readonly HttpClient _client;

    public ListAndGetProjectTests()
    {
        _factory = new TaskFlowApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task List_NoProjects_ReturnsEmptyList()
    {
        var response = await _client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectDto>>(ApiJson.Options);
        Assert.NotNull(projects);
        Assert.Empty(projects);
    }

    [Fact]
    public async Task List_SeveralProjects_OrderedByNameOrdinalThenId()
    {
        // Lowercase "alpha" is created before "Alpha", so a case-insensitive sort with an
        // id tie-break would put it first; only ordinal sorting puts "Alpha" first.
        var beta = await _client.CreateProjectAsync("beta");
        var lowerAlpha = await _client.CreateProjectAsync("alpha");
        var upperAlpha = await _client.CreateProjectAsync("Alpha");
        var secondBeta = await _client.CreateProjectAsync("beta");

        var response = await _client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectDto>>(ApiJson.Options);
        Assert.NotNull(projects);
        // Ordinal: uppercase letters sort before lowercase ones; equal names fall back to id.
        Assert.Equal(
            [upperAlpha.Id, lowerAlpha.Id, beta.Id, secondBeta.Id],
            projects.Select(p => p.Id).ToArray());
        Assert.All(projects, p => Assert.Equal(0, p.TaskCount));
    }

    [Fact]
    public async Task GetById_ExistingProject_Returns200WithDto()
    {
        var created = await _client.CreateProjectAsync("Portfolio site", "Notes");

        var response = await _client.GetAsync($"/api/projects/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectDto>(ApiJson.Options);
        Assert.Equal(new ProjectDto(created.Id, "Portfolio site", "Notes", 0), project);
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/projects/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJson.Options);
        Assert.Equal(404, problem?.Status);
    }
}
