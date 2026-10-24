using System.Net;
using System.Net.Http.Json;
using TaskFlow.Api.Dtos;

namespace TaskFlow.Api.Tests.TestInfrastructure;

/// <summary>Shortcuts for arranging test data through the real API.</summary>
public static class HttpClientExtensions
{
    /// <summary>Creates a project via POST /api/projects and returns the API's response body.</summary>
    public static async Task<ProjectDto> CreateProjectAsync(
        this HttpClient client, string name, string? description = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/projects", new SaveProjectRequest { Name = name, Description = description }, ApiJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var project = await response.Content.ReadFromJsonAsync<ProjectDto>(ApiJson.Options);
        Assert.NotNull(project);
        return project;
    }
}
