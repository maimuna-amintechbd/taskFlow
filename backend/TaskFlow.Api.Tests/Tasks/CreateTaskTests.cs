using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Api.Data;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Models;
using TaskFlow.Api.Tests.TestInfrastructure;

namespace TaskFlow.Api.Tests.Tasks;

public class CreateTaskTests : IDisposable
{
    private readonly TaskFlowApiFactory _factory;
    private readonly HttpClient _client;

    public CreateTaskTests()
    {
        _factory = new TaskFlowApiFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private Task<HttpResponseMessage> PostAsync(object body) =>
        _client.PostAsJsonAsync("/api/tasks", body, ApiJson.Options);

    private Task<HttpResponseMessage> PostRawAsync(string json) =>
        _client.PostAsync("/api/tasks", new StringContent(json, Encoding.UTF8, "application/json"));

    private static async Task<ValidationProblemDetails> ReadProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(ApiJson.Options);
        Assert.NotNull(problem);
        return problem;
    }

    /// <summary>Counts rows straight from the database, so "nothing saved" is checked at the source.</summary>
    private async Task AssertNoTasksAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaskFlowDbContext>();
        Assert.Equal(0, await db.TaskItems.CountAsync());
    }

    [Fact]
    public async Task Create_TitleOnly_Returns201WithDefaultsAndLocation()
    {
        var project = await _client.CreateProjectAsync("Portfolio");

        var response = await PostAsync(new { title = "  Write README  ", projectId = project.Id });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var task = JsonSerializer.Deserialize<TaskDto>(json, ApiJson.Options);
        Assert.NotNull(task);
        Assert.True(task.Id > 0);
        Assert.Equal($"/api/tasks/{task.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal("Write README", task.Title);
        Assert.Null(task.Description);
        Assert.Equal(TaskItemStatus.Todo, task.Status);
        Assert.Equal(TaskPriority.Medium, task.Priority);
        Assert.Null(task.DueDate);
        Assert.Equal(project.Id, task.ProjectId);
        Assert.Equal("Portfolio", task.ProjectName);
        Assert.Equal(_factory.Clock.UtcNow.UtcDateTime, task.CreatedAt);

        // The raw JSON: enums by name and a UTC timestamp ending in "Z".
        using var document = JsonDocument.Parse(json);
        Assert.Equal("Todo", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("Medium", document.RootElement.GetProperty("priority").GetString());
        Assert.EndsWith("Z", document.RootElement.GetProperty("createdAt").GetString());
    }

    [Fact]
    public async Task Create_Task_IncreasesProjectTaskCount()
    {
        var project = await _client.CreateProjectAsync("Counted");
        var other = await _client.CreateProjectAsync("Other");

        await _client.CreateTaskAsync(project.Id);

        var projects = await _client.GetFromJsonAsync<List<ProjectDto>>("/api/projects", ApiJson.Options);
        Assert.NotNull(projects);
        Assert.Equal(1, projects.Single(p => p.Id == project.Id).TaskCount);
        Assert.Equal(0, projects.Single(p => p.Id == other.Id).TaskCount);
        var single = await _client.GetFromJsonAsync<ProjectDto>($"/api/projects/{project.Id}", ApiJson.Options);
        Assert.Equal(1, single?.TaskCount);
    }

    [Theory]
    [InlineData("""{"projectId":PID}""")]
    [InlineData("""{"title":"","projectId":PID}""")]
    [InlineData("""{"title":"   ","projectId":PID}""")]
    public async Task Create_TitleMissingOrBlank_Returns400TitleRequired(string json)
    {
        var project = await _client.CreateProjectAsync("P");

        var response = await PostRawAsync(json.Replace("PID", project.Id.ToString()));

        var problem = await ReadProblemAsync(response);
        Assert.Equal(["Title is required."], problem.Errors["title"]);
        await AssertNoTasksAsync();
    }

    [Fact]
    public async Task Create_TitleTooLongAfterTrim_Returns400()
    {
        var project = await _client.CreateProjectAsync("P");

        var response = await PostAsync(new { title = $"  {new string('t', 201)}  ", projectId = project.Id });

        var problem = await ReadProblemAsync(response);
        Assert.Equal(["Title must be at most 200 characters."], problem.Errors["title"]);
        await AssertNoTasksAsync();
    }

    [Fact]
    public async Task Create_TitleAtLimitWithSurroundingSpaces_Returns201()
    {
        var project = await _client.CreateProjectAsync("P");
        var title = new string('t', 200);

        var task = await _client.CreateTaskAsync(project.Id, $"  {title}  ");

        Assert.Equal(title, task.Title);
    }

    [Fact]
    public async Task Create_ProjectMissing_Returns400ProjectRequired()
    {
        var response = await PostAsync(new { title = "Orphan" });

        var problem = await ReadProblemAsync(response);
        Assert.Equal(["Project is required."], problem.Errors["projectId"]);
        await AssertNoTasksAsync();
    }

    [Fact]
    public async Task Create_UnknownProject_Returns400ProjectDoesNotExist()
    {
        var response = await PostAsync(new { title = "Orphan", projectId = 9999 });

        var problem = await ReadProblemAsync(response);
        Assert.Equal(["Project 9999 does not exist."], problem.Errors["projectId"]);
        await AssertNoTasksAsync();
    }

    [Theory]
    [InlineData("inprogress", TaskItemStatus.InProgress)]
    [InlineData("DONE", TaskItemStatus.Done)]
    [InlineData("Todo", TaskItemStatus.Todo)]
    public async Task Create_StatusNameAnyCase_Returns201WithCanonicalName(string status, TaskItemStatus expected)
    {
        var project = await _client.CreateProjectAsync("P");

        var response = await PostAsync(new { title = "T", projectId = project.Id, status });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(expected.ToString(), document.RootElement.GetProperty("status").GetString());
    }

    [Theory]
    [InlineData("high")]
    [InlineData("HIGH")]
    [InlineData("High")]
    public async Task Create_PriorityNameAnyCase_Returns201WithCanonicalName(string priority)
    {
        var project = await _client.CreateProjectAsync("P");

        var response = await PostAsync(new { title = "T", projectId = project.Id, priority });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("High", document.RootElement.GetProperty("priority").GetString());
    }

    // Every value is close to a real name and fails only by the rule under test
    // (number, empty, whitespace, comma list, inner space, misspelling).
    [Theory]
    [InlineData("status", "7", "Status must be one of: Todo, InProgress, Done.")]
    [InlineData("status", "2", "Status must be one of: Todo, InProgress, Done.")]
    [InlineData("status", "", "Status must be one of: Todo, InProgress, Done.")]
    [InlineData("status", "Todo ", "Status must be one of: Todo, InProgress, Done.")]
    [InlineData("status", "todo,done", "Status must be one of: Todo, InProgress, Done.")]
    [InlineData("status", "In Progress", "Status must be one of: Todo, InProgress, Done.")]
    [InlineData("status", "Tood", "Status must be one of: Todo, InProgress, Done.")]
    // The exact values named in the spec's AC-3 row.
    [InlineData("status", " Todo", "Status must be one of: Todo, InProgress, Done.")]
    [InlineData("status", "Todo,Done", "Status must be one of: Todo, InProgress, Done.")]
    [InlineData("status", "Hgh", "Status must be one of: Todo, InProgress, Done.")]
    [InlineData("priority", "7", "Priority must be one of: Low, Medium, High.")]
    [InlineData("priority", "2", "Priority must be one of: Low, Medium, High.")]
    [InlineData("priority", "", "Priority must be one of: Low, Medium, High.")]
    [InlineData("priority", "High ", "Priority must be one of: Low, Medium, High.")]
    [InlineData("priority", "Low,High", "Priority must be one of: Low, Medium, High.")]
    [InlineData("priority", " High", "Priority must be one of: Low, Medium, High.")]
    [InlineData("priority", "Hihg", "Priority must be one of: Low, Medium, High.")]
    public async Task Create_InvalidStatusOrPriorityName_Returns400(string field, string value, string message)
    {
        var project = await _client.CreateProjectAsync("P");
        var body = new Dictionary<string, object> { ["title"] = "T", ["projectId"] = project.Id, [field] = value };

        var response = await PostAsync(body);

        var problem = await ReadProblemAsync(response);
        Assert.Equal([message], problem.Errors[field]);
        Assert.Single(problem.Errors);
        await AssertNoTasksAsync();
    }

    [Fact]
    public async Task Create_StatusAsJsonNumber_Returns400FromBinder()
    {
        var project = await _client.CreateProjectAsync("P");

        var response = await PostRawAsync($$"""{"title":"T","projectId":{{project.Id}},"status":2}""");

        var problem = await ReadProblemAsync(response);
        Assert.True(problem.Errors.ContainsKey("$.status"));
        await AssertNoTasksAsync();
    }

    [Fact]
    public async Task Create_DueDateYesterday_Returns400()
    {
        var project = await _client.CreateProjectAsync("P");

        var response = await PostAsync(new { title = "T", projectId = project.Id, dueDate = _factory.Clock.Today.AddDays(-1) });

        var problem = await ReadProblemAsync(response);
        Assert.Equal(["Due date cannot be in the past."], problem.Errors["dueDate"]);
        await AssertNoTasksAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(null)]
    public async Task Create_DueDateTodayTomorrowOrNone_Returns201(int? daysFromToday)
    {
        var project = await _client.CreateProjectAsync("P");
        DateOnly? dueDate = daysFromToday is { } days ? _factory.Clock.Today.AddDays(days) : null;

        var task = await _client.CreateTaskAsync(project.Id, dueDate: dueDate);

        Assert.Equal(dueDate, task.DueDate);
    }

    [Fact]
    public async Task Create_DueDateOnMachineWhoseLocalDateIsAheadOfUtc_UsesLocalDate()
    {
        // 23:30 UTC on 15 June is already 04:30 on 16 June at UTC+5, so "today" is the 16th.
        _factory.Clock.LocalZone = TimeZoneInfo.CreateCustomTimeZone("UTC+5", TimeSpan.FromHours(5), "UTC+5", "UTC+5");
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 6, 15, 23, 30, 0, TimeSpan.Zero);
        var project = await _client.CreateProjectAsync("P");

        var past = await PostAsync(new { title = "T", projectId = project.Id, dueDate = new DateOnly(2026, 6, 15) });
        var task = await _client.CreateTaskAsync(project.Id, dueDate: new DateOnly(2026, 6, 16));

        var problem = await ReadProblemAsync(past);
        Assert.Equal(["Due date cannot be in the past."], problem.Errors["dueDate"]);
        Assert.Equal(new DateOnly(2026, 6, 16), task.DueDate);
    }

    [Fact]
    public async Task Create_WhitespaceOnlyDescription_IsStoredAsNull()
    {
        var project = await _client.CreateProjectAsync("P");

        var created = await _client.CreateTaskAsync(project.Id, description: "   ");

        var saved = await _client.GetFromJsonAsync<TaskDto>($"/api/tasks/{created.Id}", ApiJson.Options);
        Assert.Null(created.Description);
        Assert.Null(saved?.Description);
    }

    [Fact]
    public async Task Create_DescriptionWithSpaces_IsStoredTrimmed()
    {
        var project = await _client.CreateProjectAsync("P");

        var created = await _client.CreateTaskAsync(project.Id, description: "  Notes  ");

        var saved = await _client.GetFromJsonAsync<TaskDto>($"/api/tasks/{created.Id}", ApiJson.Options);
        Assert.Equal("Notes", created.Description);
        Assert.Equal("Notes", saved?.Description);
    }

    [Fact]
    public async Task Create_DescriptionAtLimitWithSurroundingSpaces_Returns201()
    {
        var project = await _client.CreateProjectAsync("P");
        var description = new string('d', 2000);

        var created = await _client.CreateTaskAsync(project.Id, description: $"  {description}  ");

        var saved = await _client.GetFromJsonAsync<TaskDto>($"/api/tasks/{created.Id}", ApiJson.Options);
        Assert.Equal(description, saved?.Description);
    }

    [Fact]
    public async Task Create_DescriptionTooLong_Returns400()
    {
        var project = await _client.CreateProjectAsync("P");

        var response = await PostAsync(new { title = "T", projectId = project.Id, description = new string('d', 2001) });

        var problem = await ReadProblemAsync(response);
        Assert.Equal(["Description must be at most 2000 characters."], problem.Errors["description"]);
        Assert.Single(problem.Errors);
        await AssertNoTasksAsync();
    }

    [Fact]
    public async Task Create_SeveralInvalidFields_Returns400WithEveryErrorAtOnce()
    {
        var response = await PostAsync(new
        {
            title = "",
            projectId = 9999,
            status = "7",
            description = new string('d', 2001),
        });

        var problem = await ReadProblemAsync(response);
        Assert.Equal(["Title is required."], problem.Errors["title"]);
        Assert.Equal(["Project 9999 does not exist."], problem.Errors["projectId"]);
        Assert.Equal(["Status must be one of: Todo, InProgress, Done."], problem.Errors["status"]);
        Assert.Equal(["Description must be at most 2000 characters."], problem.Errors["description"]);
        Assert.Equal(4, problem.Errors.Count);
        await AssertNoTasksAsync();
    }
}
