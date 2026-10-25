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

    /// <summary>Creates a task via POST /api/tasks and returns the API's response body.</summary>
    public static async Task<TaskDto> CreateTaskAsync(
        this HttpClient client,
        int projectId,
        string title = "A task",
        string? status = null,
        string? priority = null,
        DateOnly? dueDate = null,
        string? description = null)
    {
        var request = new SaveTaskRequest
        {
            Title = title,
            Description = description,
            ProjectId = projectId,
            Status = status,
            Priority = priority,
            DueDate = dueDate,
        };
        var response = await client.PostAsJsonAsync("/api/tasks", request, ApiJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskDto>(ApiJson.Options);
        Assert.NotNull(task);
        return task;
    }
}
