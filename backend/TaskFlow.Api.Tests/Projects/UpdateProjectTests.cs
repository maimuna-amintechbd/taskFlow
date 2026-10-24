using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests.Projects;

public class UpdateProjectTests : IDisposable
{
    private readonly TaskFlowApiFactory _factory;
    private readonly HttpClient _client;

    public UpdateProjectTests()
    {
        _factory = new TaskFlowApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private Task<HttpResponseMessage> PutAsync(int id, object body) =>
        _client.PutAsJsonAsync($"/api/projects/{id}", body, ApiJson.Options);

    private Task<ProjectDto?> GetAsync(int id) =>
        _client.GetFromJsonAsync<ProjectDto>($"/api/projects/{id}", ApiJson.Options);

    private static async Task AssertNotFoundProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJson.Options);
        Assert.Equal(404, problem?.Status);
    }

    [Fact]
    public async Task Update_ValidBodyWithSpaces_Returns200WithTrimmedValues()
    {
        var created = await _client.CreateProjectAsync("Old", "Old description");

        var response = await PutAsync(created.Id, new { name = "  New  ", description = "  d  " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProjectDto>(ApiJson.Options);
        Assert.Equal(new ProjectDto(created.Id, "New", "d", 0), updated);
        Assert.Equal(updated, await GetAsync(created.Id));
    }

    [Fact]
    public async Task Update_DescriptionOmitted_ClearsDescription()
    {
        var created = await _client.CreateProjectAsync("Notes", "Will be cleared");

        var response = await PutAsync(created.Id, new { name = "Notes" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ProjectDto>(ApiJson.Options);
        Assert.Null(updated?.Description);
        Assert.Null((await GetAsync(created.Id))?.Description);
    }

    [Theory]
    [InlineData(0, "Name is required.")]
    [InlineData(101, "Name must be at most 100 characters.")]
    public async Task Update_InvalidName_Returns400AndSavesNothing(int nameLength, string expectedMessage)
    {
        var name = new string('a', nameLength);
        var created = await _client.CreateProjectAsync("Keep me", "Keep this too");

        var response = await PutAsync(created.Id, new { name, description = "Changed" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiJson.Options);
        Assert.NotNull(problem);
        Assert.Equal([expectedMessage], problem.Errors["name"]);
        Assert.Equal(created, await GetAsync(created.Id));
    }

    [Fact]
    public async Task Update_UnknownIdValidBody_Returns404ProblemDetails()
    {
        var response = await PutAsync(9999, new { name = "Valid" });

        await AssertNotFoundProblemAsync(response);
    }

    [Fact]
    public async Task Update_UnknownIdInvalidBody_Returns404Not400()
    {
        var response = await PutAsync(9999, new { name = "" });

        await AssertNotFoundProblemAsync(response);
    }
}
