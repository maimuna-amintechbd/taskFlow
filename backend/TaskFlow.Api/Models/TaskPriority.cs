namespace TaskFlow.Api.Models;

/// <summary>How urgent a task is. Stored as text and sent over JSON by name.</summary>
public enum TaskPriority
{
    Low,
    Medium,
    High,
}
