namespace TaskFlow.Api.Dtos;

/// <summary>
/// Body of POST /api/tasks (and, from Story 3.3, PUT /api/tasks/{id}).
/// Every property is nullable so a missing field reaches TaskService, which reports
/// it with the task's own message. Status and priority are strings, not enums, so
/// TaskService can apply the strict name rules (see EnumNames) and its own messages.
/// </summary>
public class SaveTaskRequest
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? ProjectId { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    // Sent as "yyyy-MM-dd"; DateOnly carries no time of day or time zone.
    public DateOnly? DueDate { get; set; }
}
