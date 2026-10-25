namespace TaskFlow.Api.Models;

/// <summary>
/// A task inside a project. Named TaskItem (not Task) so it never clashes with
/// System.Threading.Tasks.Task. EF Core maps it to the TaskItems table.
/// </summary>
public class TaskItem
{
    public int Id { get; set; }

    // Always set by TaskService (trimmed, 1–200 characters).
    public string Title { get; set; } = "";

    // Null when the user gave no description (or only whitespace).
    public string? Description { get; set; }

    // Same defaults as the API, so a TaskItem built anywhere else doesn't silently
    // get enum value 0 (which would be Low for priority).
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Todo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    // DateOnly is a calendar date with no time of day; null means "no due date".
    public DateOnly? DueDate { get; set; }

    // Set once on create, always in UTC (see the converter in TaskFlowDbContext).
    public DateTime CreatedAt { get; set; }

    // The foreign key. EF Core enforces it in SQLite, and deleting the project
    // deletes this row through ON DELETE CASCADE.
    public int ProjectId { get; set; }

    // Navigation property. "= null!" tells the compiler "EF Core fills this in";
    // it is only loaded when a query asks for it (e.g. p.Project.Name in a Select).
    public Project Project { get; set; } = null!;
}
