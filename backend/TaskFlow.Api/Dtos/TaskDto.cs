using TaskFlow.Api.Models;

namespace TaskFlow.Api.Dtos;

/// <summary>
/// The task shape the API returns. Status and priority are serialized by name
/// ("InProgress", "High") through the global JsonStringEnumConverter.
/// CreatedAt is UTC, so its JSON ends in "Z".
/// </summary>
public record TaskDto(
    int Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    DateOnly? DueDate,
    int ProjectId,
    string ProjectName,
    DateTime CreatedAt);
