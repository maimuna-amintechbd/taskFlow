namespace TaskFlow.Api.Models;

/// <summary>
/// A project groups related tasks. EF Core maps this class to the Projects table.
/// Story 3.1 adds the Tasks collection and the cascade delete.
/// </summary>
public class Project
{
    // By convention, an int property named Id becomes the auto-incrementing primary key.
    public int Id { get; set; }

    // Initialized to "" so the non-nullable property is never null; the service always sets it.
    public string Name { get; set; } = "";

    // Nullable: a project may have no description (stored as NULL).
    public string? Description { get; set; }
}
