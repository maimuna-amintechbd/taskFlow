using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests.Projects;

public class CreateProjectTests : IDisposable
{
    private readonly TaskFlowApiFactory _factory;
    private readonly HttpClient _client;

    public CreateProjectTests()
    {
        _factory = new TaskFlowApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private Task<HttpResponseMessage> PostAsync(object body) =>
        _client.PostAsJsonAsync("/api/projects", body, ApiJson.Options);

    private static async Task<ValidationProblemDetails> ReadProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiJson.Options);
        Assert.NotNull(problem);
        return problem;
    }

    private async Task AssertNoProjectsAsync()
    {
        var projects = await _client.GetFromJsonAsync<List<ProjectDto>>("/api/projects", ApiJson.Options);
        Assert.NotNull(projects);
        Assert.Empty(projects);
    }

    [Fact]
    public async Task Create_TrimmedNameAndBlankDescription_Returns201WithTrimmedValues()
    {
        var response = await PostAsync(new { name = "  Portfolio site  ", description = "  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectDto>(ApiJson.Options);
        Assert.NotNull(project);
        Assert.True(project.Id > 0);
        Assert.Equal("Portfolio site", project.Name);
        Assert.Null(project.Description);
        Assert.Equal(0, project.TaskCount);
        Assert.Equal($"/api/projects/{project.Id}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task Create_DescriptionWithSpaces_SavesTrimmedDescription()
    {
        var created = await _client.CreateProjectAsync("Notes", "  My notes  ");

        var saved = await _client.GetFromJsonAsync<ProjectDto>($"/api/projects/{created.Id}", ApiJson.Options);
        Assert.Equal("My notes", created.Description);
        Assert.Equal("My notes", saved?.Description);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"name":""}""")]
    [InlineData("""{"name":"   "}""")]
    public async Task Create_NameMissingOrBlank_Returns400NameRequired(string json)
    {
        var response = await _client.PostAsync(
            "/api/projects", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        var problem = await ReadProblemAsync(response);
        Assert.Equal(["Name is required."], problem.Errors["name"]);
        await AssertNoProjectsAsync();
    }

    [Fact]
    public async Task Create_NameTooLong_Returns400()
    {
        var response = await PostAsync(new { name = new string('a', 101) });

        var problem = await ReadProblemAsync(response);
        Assert.Equal(["Name must be at most 100 characters."], problem.Errors["name"]);
        await AssertNoProjectsAsync();
    }

    [Fact]
    public async Task Create_NameAtLimitWithSurroundingSpaces_Returns201()
    {
        var name = new string('a', 100);

        var response = await PostAsync(new { name = $"  {name}  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectDto>(ApiJson.Options);
        Assert.Equal(name, project?.Name);
        Assert.Null(project?.Description);
    }

    [Fact]
    public async Task Create_DescriptionAtLimitWithSurroundingSpaces_Returns201()
    {
        var description = new string('d', 1000);

        var response = await PostAsync(new { name = "Valid", description = $"  {description}  " });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ProjectDto>(ApiJson.Options);
        Assert.NotNull(created);
        var saved = await _client.GetFromJsonAsync<ProjectDto>($"/api/projects/{created.Id}", ApiJson.Options);
        Assert.Equal(description, saved?.Description);
    }

    [Fact]
    public async Task Create_DescriptionTooLong_Returns400()
    {
        var response = await PostAsync(new { name = "Valid", description = new string('d', 1001) });

        var problem = await ReadProblemAsync(response);
        Assert.Equal(["Description must be at most 1000 characters."], problem.Errors["description"]);
        Assert.False(problem.Errors.ContainsKey("name"));
        await AssertNoProjectsAsync();
    }

    [Fact]
    public async Task Create_EmptyNameAndLongDescription_Returns400WithBothErrors()
    {
        var response = await PostAsync(new { name = "", description = new string('d', 1001) });

        var problem = await ReadProblemAsync(response);
        Assert.Equal(["Name is required."], problem.Errors["name"]);
        Assert.Equal(["Description must be at most 1000 characters."], problem.Errors["description"]);
        Assert.Equal(2, problem.Errors.Count);
        await AssertNoProjectsAsync();
    }

    [Fact]
    public async Task Create_DuplicateName_BothReturn201()
    {
        var first = await _client.CreateProjectAsync("Same name");
        var second = await _client.CreateProjectAsync("Same name");

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public async Task Create_EmptyBody_Returns400()
    {
        var response = await _client.PostAsync(
            "/api/projects", new StringContent("", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertNoProjectsAsync();
    }
}
