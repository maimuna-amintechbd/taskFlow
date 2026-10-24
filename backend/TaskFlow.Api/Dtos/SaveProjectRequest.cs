namespace TaskFlow.Api.Dtos;

/// <summary>
/// Body of POST (and, in Story 2.2, PUT) /api/projects.
/// Every property is nullable so a missing field reaches ProjectService,
/// which reports it with the project's own message instead of a framework one.
/// </summary>
public class SaveProjectRequest
{
    public string? Name { get; set; }

    public string? Description { get; set; }
}
