namespace TaskFlow.Api.Models;

/// <summary>
/// Where a task is in its life. Stored as text ("Todo", "InProgress", "Done") and
/// sent over JSON by name. Named TaskItemStatus, not TaskStatus, to avoid clashing
/// with System.Threading.Tasks.TaskStatus.
/// </summary>
public enum TaskItemStatus
{
    Todo,
    InProgress,
    Done,
}
