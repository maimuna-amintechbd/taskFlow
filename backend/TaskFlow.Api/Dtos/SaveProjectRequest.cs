namespace TaskFlow.Api.Dtos;

/// <summary>
/// Body of POST /api/projects and PUT /api/projects/{id}.
/// Every property is nullable so a missing field reaches ProjectService,
/// which reports it with the project's own message instead of a framework one.
/// </summary>
public class SaveProjectRequest
{
    public string? Name { get; set; }

    public string? Description { get; set; }
}
