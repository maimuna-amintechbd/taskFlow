namespace TaskFlow.Api.Models;

/// <summary>
/// A project groups related tasks. EF Core maps this class to the Projects table.
/// Deleting a project deletes its tasks (cascade configured in TaskFlowDbContext).
/// </summary>
public class Project
{
    // By convention, an int property named Id becomes the auto-incrementing primary key.
    public int Id { get; set; }

    // Initialized to "" so the non-nullable property is never null; the service always sets it.
    public string Name { get; set; } = "";

    // Nullable: a project may have no description (stored as NULL).
    public string? Description { get; set; }

    // The project's tasks. Never loaded just to count them: queries use p.Tasks.Count(),
    // which EF Core turns into a SQL COUNT subquery.
    public List<TaskItem> Tasks { get; set; } = [];
}
