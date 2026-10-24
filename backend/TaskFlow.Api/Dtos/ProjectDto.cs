namespace TaskFlow.Api.Dtos;

/// <summary>
/// The project shape the API returns. A record is an immutable class with
/// value equality, which suits data that only travels out of the API.
/// </summary>
public record ProjectDto(int Id, string Name, string? Description, int TaskCount);
