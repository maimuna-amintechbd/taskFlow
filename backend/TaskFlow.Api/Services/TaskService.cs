using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Services;

/// <summary>
/// Owns the task rules and every EF Core query on tasks. Registered as scoped, so it
/// shares the request's TaskFlowDbContext.
/// TimeProvider is .NET's injectable clock: production gets TimeProvider.System,
/// tests swap in a fixed clock so "today" is predictable.
/// </summary>
public class TaskService(TaskFlowDbContext db, TimeProvider timeProvider)
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    /// <summary>
    /// The one place a TaskItem becomes a TaskDto. An expression tree, so EF Core
    /// translates it into SQL, including the join for the project name.
    /// </summary>
    private static readonly Expression<Func<TaskItem, TaskDto>> ToDto =
        t => new TaskDto(
            t.Id,
            t.Title,
            t.Description,
            t.Status,
            t.Priority,
            t.DueDate,
            t.ProjectId,
            t.Project.Name,
            t.CreatedAt);

    /// <summary>
    /// Every task, newest first (id breaks ties between tasks created at the same instant).
    /// An optional project filter must be a whole number naming an existing project;
    /// otherwise the result is Invalid under "projectId", never an empty list.
    /// </summary>
    public async Task<ServiceResult<List<TaskDto>>> ListAsync(TaskListQuery query)
    {
        var tasks = db.TaskItems.AsNoTracking();

        if (query.ProjectId is not null)
        {
            // AllowLeadingSign + InvariantCulture: digits with an optional sign only.
            // Spaces, decimals, thousands separators and "" are rejected.
            if (!int.TryParse(query.ProjectId, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var projectId))
            {
                return ServiceResult<List<TaskDto>>.Invalid(
                    new() { ["projectId"] = ["Project id must be a whole number."] });
            }

            if (!await db.Projects.AnyAsync(p => p.Id == projectId))
            {
                return ServiceResult<List<TaskDto>>.Invalid(
                    new() { ["projectId"] = [$"Project {projectId} does not exist."] });
            }

            tasks = tasks.Where(t => t.ProjectId == projectId);
        }

        var dtos = await tasks
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Select(ToDto)
            .ToListAsync();
        return ServiceResult<List<TaskDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<TaskDto>> GetByIdAsync(int id)
    {
        var dto = await FindDtoAsync(id);
        return dto is null ? ServiceResult<TaskDto>.NotFound() : ServiceResult<TaskDto>.Ok(dto);
    }

    public async Task<ServiceResult<TaskDto>> CreateAsync(SaveTaskRequest request)
    {
        var task = new TaskItem();
        var errors = await ValidateAndApplyAsync(request, task, isNew: true);
        if (errors.Count > 0)
        {
            return ServiceResult<TaskDto>.Invalid(errors);
        }

        task.CreatedAt = timeProvider.GetUtcNow().UtcDateTime;
        db.TaskItems.Add(task);
        await db.SaveChangesAsync();

        // Re-read through the same projection so every response has one shape.
        var dto = await FindDtoAsync(task.Id);
        return ServiceResult<TaskDto>.Ok(dto!);
    }

    private Task<TaskDto?> FindDtoAsync(int id) =>
        db.TaskItems
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(ToDto)
            .SingleOrDefaultAsync();

    /// <summary>
    /// Checks every rule in one pass (at most one message per field) and copies the
    /// values onto <paramref name="task"/> only when there are no errors. Shared by
    /// create and (from Story 3.3) update; <paramref name="isNew"/> only switches the
    /// status/priority defaults and the due-date comparison.
    /// </summary>
    private async Task<Dictionary<string, string[]>> ValidateAndApplyAsync(
        SaveTaskRequest request, TaskItem task, bool isNew)
    {
        // Only create is wired up so far. The edit rules (required status/priority,
        // keeping an unchanged past due date) arrive with PUT in Story 3.3.
        if (!isNew)
        {
            throw new NotSupportedException("Validating an edit (isNew: false) arrives in Story 3.3.");
        }

        var errors = new Dictionary<string, string[]>();

        var title = request.Title?.Trim() ?? "";
        if (title.Length == 0)
        {
            errors["title"] = ["Title is required."];
        }
        else if (title.Length > TitleMaxLength)
        {
            errors["title"] = [$"Title must be at most {TitleMaxLength} characters."];
        }

        // A whitespace-only description is stored as null.
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (description is not null && description.Length > DescriptionMaxLength)
        {
            errors["description"] = [$"Description must be at most {DescriptionMaxLength} characters."];
        }

        // An unknown project is a validation error (400), not a 404: the URL was fine,
        // one field of the body was wrong.
        if (request.ProjectId is not { } projectId)
        {
            errors["projectId"] = ["Project is required."];
        }
        else if (!await db.Projects.AnyAsync(p => p.Id == projectId))
        {
            errors["projectId"] = [$"Project {projectId} does not exist."];
        }

        // On create, a missing (null) status/priority takes the default; a value that
        // is present but empty or not an exact name is an error.
        var status = TaskItemStatus.Todo;
        if (request.Status is not null && !EnumNames.TryParse(request.Status, out status))
        {
            errors["status"] = [$"Status must be one of: {EnumNames.List<TaskItemStatus>()}."];
        }

        var priority = TaskPriority.Medium;
        if (request.Priority is not null && !EnumNames.TryParse(request.Priority, out priority))
        {
            errors["priority"] = [$"Priority must be one of: {EnumNames.List<TaskPriority>()}."];
        }

        // "Today" is the backend machine's local date, read from the injected clock.
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        if (request.DueDate is { } dueDate && dueDate < today)
        {
            errors["dueDate"] = ["Due date cannot be in the past."];
        }

        if (errors.Count == 0)
        {
            task.Title = title;
            task.Description = description;
            task.ProjectId = request.ProjectId!.Value;
            task.Status = status;
            task.Priority = priority;
            task.DueDate = request.DueDate;
        }

        return errors;
    }
}
