namespace TaskFlow.Api.Dtos;

/// <summary>
/// The dashboard totals across all projects. Total = Completed + Pending:
/// Completed counts Done tasks, Pending counts Todo and InProgress tasks.
/// </summary>
public record DashboardDto(int Total, int Completed, int Pending);
