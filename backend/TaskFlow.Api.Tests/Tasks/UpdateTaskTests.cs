using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Models;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests.Tasks;

public class UpdateTaskTests : IDisposable
{
    private readonly TaskFlowApiFactory _factory;
    private readonly HttpClient _client;

    public UpdateTaskTests()
    {
        _factory = new TaskFlowApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private Task<HttpResponseMessage> PutAsync(int id, object body) =>
        _client.PutAsJsonAsync($"/api/tasks/{id}", body, ApiJson.Options);

    private Task<HttpResponseMessage> PutRawAsync(int id, string json) =>
        _client.PutAsync($"/api/tasks/{id}", new StringContent(json, Encoding.UTF8, "application/json"));

    private Task<TaskDto?> GetAsync(int id) =>
        _client.GetFromJsonAsync<TaskDto>($"/api/tasks/{id}", ApiJson.Options);

    private static async Task<TaskDto> ReadTaskAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskDto>(ApiJson.Options);
        Assert.NotNull(task);
        return task;
    }

    private static async Task<ValidationProblemDetails> ReadProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiJson.Options);
        Assert.NotNull(problem);
        return problem;
    }

    private static async Task AssertNotFoundProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ApiJson.Options);
        Assert.Equal(404, problem?.Status);
    }

    /// <summary>A project with one task carrying a value in every optional field.</summary>
    private async Task<TaskDto> CreateFullTaskAsync()
    {
        var project = await _client.CreateProjectAsync("P");
        return await _client.CreateTaskAsync(
            project.Id,
            title: "Original",
            status: "InProgress",
            priority: "High",
            dueDate: _factory.Clock.Today.AddDays(3),
            description: "Original notes");
    }

    /// <summary>A complete, valid PUT body for <paramref name="task"/> that changes nothing.</summary>
    private static Dictionary<string, object?> BodyFrom(TaskDto task) => new()
    {
        ["title"] = task.Title,
        ["description"] = task.Description,
        ["projectId"] = task.ProjectId,
        ["status"] = task.Status.ToString(),
        ["priority"] = task.Priority.ToString(),
        ["dueDate"] = task.DueDate?.ToString("yyyy-MM-dd"),
    };

    [Fact]
    public async Task Update_EveryField_Returns200WithReReadValues()
    {
        var created = await CreateFullTaskAsync();
        var dueDate = _factory.Clock.Today.AddDays(10);

        var response = await PutAsync(created.Id, new
        {
            title = "  Renamed  ",
            description = "  New notes  ",
            projectId = created.ProjectId,
            status = "done",
            priority = "low",
            dueDate,
        });

        var updated = await ReadTaskAsync(response);
        var expected = created with
        {
            Title = "Renamed",
            Description = "New notes",
            Status = TaskItemStatus.Done,
            Priority = TaskPriority.Low,
            DueDate = dueDate,
        };
        Assert.Equal(expected, updated);
        Assert.Equal(expected, await GetAsync(created.Id));
    }

    [Fact]
    public async Task Update_MoveToAnotherProject_ChangesProjectNameAndTaskCounts()
    {
        var p = await _client.CreateProjectAsync("Pea");
        var q = await _client.CreateProjectAsync("Queue");
        var task = await _client.CreateTaskAsync(p.Id, title: "Mover");
        var body = BodyFrom(task);
        body["projectId"] = q.Id;

        var updated = await ReadTaskAsync(await PutAsync(task.Id, body));

        Assert.Equal(q.Id, updated.ProjectId);
        Assert.Equal("Queue", updated.ProjectName);
        var projects = await _client.GetFromJsonAsync<List<ProjectDto>>("/api/projects", ApiJson.Options);
        Assert.NotNull(projects);
        Assert.Equal(0, projects.Single(x => x.Id == p.Id).TaskCount);
        Assert.Equal(1, projects.Single(x => x.Id == q.Id).TaskCount);
    }

    [Theory]
    [InlineData("status", "Status is required.")]
    [InlineData("priority", "Priority is required.")]
    [InlineData("title", "Title is required.")]
    [InlineData("projectId", "Project is required.")]
    public async Task Update_RequiredFieldNull_Returns400AndSavesNothing(string field, string message)
    {
        var created = await CreateFullTaskAsync();
        var body = BodyFrom(created);
        body[field] = null;
        body["description"] = "Should not be saved";

        var problem = await ReadProblemAsync(await PutAsync(created.Id, body));

        Assert.Equal([message], problem.Errors[field]);
        Assert.Single(problem.Errors);
        Assert.Equal(created, await GetAsync(created.Id));
    }

    [Theory]
    [InlineData("status", "Status is required.")]
    [InlineData("priority", "Priority is required.")]
    [InlineData("title", "Title is required.")]
    [InlineData("projectId", "Project is required.")]
    public async Task Update_RequiredFieldOmitted_Returns400AndSavesNothing(string field, string message)
    {
        var created = await CreateFullTaskAsync();
        var body = BodyFrom(created);
        body.Remove(field);

        var problem = await ReadProblemAsync(await PutAsync(created.Id, body));

        Assert.Equal([message], problem.Errors[field]);
        Assert.Single(problem.Errors);
        Assert.Equal(created, await GetAsync(created.Id));
    }

    [Theory]
    [InlineData("omit")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Update_DescriptionOmittedNullOrBlank_ClearsDescription(string? description)
    {
        var created = await CreateFullTaskAsync();
        var body = BodyFrom(created);
        if (description == "omit") body.Remove("description");
        else body["description"] = description;

        var updated = await ReadTaskAsync(await PutAsync(created.Id, body));

        Assert.Null(updated.Description);
        Assert.Null((await GetAsync(created.Id))?.Description);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Update_DueDateOmittedOrNull_ClearsDueDate(bool omit)
    {
        var created = await CreateFullTaskAsync();
        var body = BodyFrom(created);
        if (omit) body.Remove("dueDate");
        else body["dueDate"] = null;

        var updated = await ReadTaskAsync(await PutAsync(created.Id, body));

        Assert.Null(updated.DueDate);
        Assert.Null((await GetAsync(created.Id))?.DueDate);
    }

    [Fact]
    public async Task Update_RawEmptyDueDate_Returns400FromBinder()
    {
        var created = await CreateFullTaskAsync();

        var response = await PutRawAsync(
            created.Id,
            $$"""{"title":"T","projectId":{{created.ProjectId}},"status":"Todo","priority":"Low","dueDate":""}""");

        var problem = await ReadProblemAsync(response);
        Assert.True(problem.Errors.ContainsKey("$.dueDate"));
        Assert.Equal(created, await GetAsync(created.Id));
    }

    // The create rules apply unchanged: AC-1 (title), AC-2 (project), AC-3 (status).
    [Theory]
    [InlineData("title", "   ", "Title is required.")]
    [InlineData("projectId", 9999, "Project 9999 does not exist.")]
    [InlineData("status", "7", "Status must be one of: Todo, InProgress, Done.")]
    [InlineData("priority", "Hihg", "Priority must be one of: Low, Medium, High.")]
    public async Task Update_InvalidValueSameAsCreate_Returns400AndSavesNothing(string field, object value, string message)
    {
        var created = await CreateFullTaskAsync();
        var body = BodyFrom(created);
        body[field] = value;

        var problem = await ReadProblemAsync(await PutAsync(created.Id, body));

        Assert.Equal([message], problem.Errors[field]);
        Assert.Single(problem.Errors);
        Assert.Equal(created, await GetAsync(created.Id));
    }

    [Fact]
    public async Task Update_TitleTooLong_Returns400AndSavesNothing()
    {
        var created = await CreateFullTaskAsync();
        var body = BodyFrom(created);
        body["title"] = new string('t', 201);

        var problem = await ReadProblemAsync(await PutAsync(created.Id, body));

        Assert.Equal(["Title must be at most 200 characters."], problem.Errors["title"]);
        Assert.Single(problem.Errors);
        Assert.Equal(created, await GetAsync(created.Id));
    }

    [Fact]
    public async Task Update_DescriptionTooLong_Returns400AndSavesNothing()
    {
        var created = await CreateFullTaskAsync();
        var body = BodyFrom(created);
        body["description"] = new string('d', 2001);

        var problem = await ReadProblemAsync(await PutAsync(created.Id, body));

        Assert.Equal(["Description must be at most 2000 characters."], problem.Errors["description"]);
        Assert.Single(problem.Errors);
        Assert.Equal(created, await GetAsync(created.Id));
    }

    // An empty string is a present-but-invalid value, not a missing one.
    [Theory]
    [InlineData("status", "Status must be one of: Todo, InProgress, Done.")]
    [InlineData("priority", "Priority must be one of: Low, Medium, High.")]
    public async Task Update_EmptyStatusOrPriority_Returns400MustBeOneOf(string field, string message)
    {
        var created = await CreateFullTaskAsync();
        var body = BodyFrom(created);
        body[field] = "";

        var problem = await ReadProblemAsync(await PutAsync(created.Id, body));

        Assert.Equal([message], problem.Errors[field]);
        Assert.Single(problem.Errors);
        Assert.Equal(created, await GetAsync(created.Id));
    }

    /// <summary>Creates a task due today, then moves the clock two days on, so its date is now past.</summary>
    private async Task<TaskDto> CreateNowOverdueTaskAsync()
    {
        var project = await _client.CreateProjectAsync("P");
        var task = await _client.CreateTaskAsync(project.Id, dueDate: _factory.Clock.Today);
        _factory.Clock.UtcNow = _factory.Clock.UtcNow.AddDays(2);
        return task;
    }

    [Fact]
    public async Task Update_UnchangedPastDueDate_Returns200()
    {
        var created = await CreateNowOverdueTaskAsync();
        var body = BodyFrom(created);
        body["title"] = "Still overdue";

        var updated = await ReadTaskAsync(await PutAsync(created.Id, body));

        Assert.Equal(created.DueDate, updated.DueDate);
        Assert.Equal("Still overdue", updated.Title);
    }

    [Fact]
    public async Task Update_NewPastDueDate_Returns400AndSavesNothing()
    {
        var created = await CreateNowOverdueTaskAsync();
        var body = BodyFrom(created);
        body["dueDate"] = _factory.Clock.Today.AddDays(-1).ToString("yyyy-MM-dd");

        var problem = await ReadProblemAsync(await PutAsync(created.Id, body));

        Assert.Equal(["Due date cannot be in the past."], problem.Errors["dueDate"]);
        Assert.Single(problem.Errors);
        Assert.Equal(created, await GetAsync(created.Id));
    }

    [Fact]
    public async Task Update_PastDueDateCleared_Returns200()
    {
        var created = await CreateNowOverdueTaskAsync();
        var body = BodyFrom(created);
        body["dueDate"] = null;

        var updated = await ReadTaskAsync(await PutAsync(created.Id, body));

        Assert.Null(updated.DueDate);
    }

    [Fact]
    public async Task Update_PastDueDateMovedToFuture_Returns200()
    {
        var created = await CreateNowOverdueTaskAsync();
        var future = _factory.Clock.Today.AddDays(5);
        var body = BodyFrom(created);
        body["dueDate"] = future.ToString("yyyy-MM-dd");

        var updated = await ReadTaskAsync(await PutAsync(created.Id, body));

        Assert.Equal(future, updated.DueDate);
        Assert.Equal(future, (await GetAsync(created.Id))?.DueDate);
    }

    [Fact]
    public async Task Update_UnknownIdValidBody_Returns404ProblemDetails()
    {
        var project = await _client.CreateProjectAsync("P");

        var response = await PutAsync(9999, new { title = "Valid", projectId = project.Id, status = "Todo", priority = "Low" });

        await AssertNotFoundProblemAsync(response);
    }

    [Fact]
    public async Task Update_UnknownIdInvalidBody_Returns404Not400()
    {
        var response = await PutAsync(9999, new { title = "" });

        await AssertNotFoundProblemAsync(response);
    }
}
