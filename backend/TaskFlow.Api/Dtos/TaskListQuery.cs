using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Api.Dtos;

/// <summary>
/// Query string of GET /api/tasks. Leaving a parameter out means "All"; the filters
/// that are present are AND-combined.
/// Properties are strings, not ints or enums, so a bad value ("abc", "7") reaches
/// TaskService, which reports it with the task's own message instead of a framework one.
/// Keep this a class with { get; set; } properties: MVC ignores [DisplayFormat] on the
/// parameters of a positional record.
/// </summary>
public class TaskListQuery
{
    // ASP.NET Core turns an empty query value ("?projectId=") into null by default,
    // which would look like "no filter". Keeping "" lets TaskService reject it.
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? ProjectId { get; set; }

    /// <summary>A TaskItemStatus name, ignoring case ("todo", "InProgress").</summary>
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Status { get; set; }

    /// <summary>A TaskPriority name, ignoring case ("HIGH", "low").</summary>
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? Priority { get; set; }
}
