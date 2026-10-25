using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Api.Dtos;

/// <summary>
/// Query string of GET /api/tasks. Leaving a parameter out means "All".
/// Properties are strings, not ints, so a bad value ("abc") reaches TaskService,
/// which reports it with the task's own message instead of a framework one.
/// Story 4.1 adds Status and Priority here.
/// </summary>
public class TaskListQuery
{
    // ASP.NET Core turns an empty query value ("?projectId=") into null by default,
    // which would look like "no filter". Keeping "" lets TaskService reject it.
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? ProjectId { get; set; }
}
