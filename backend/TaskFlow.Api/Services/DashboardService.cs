using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Services;

/// <summary>
/// Counts tasks for the dashboard. Nothing is cached or stored: every call asks the
/// database again, so the totals always match the saved data.
/// Registered as scoped, so it shares the request's TaskFlowDbContext.
/// </summary>
public class DashboardService(TaskFlowDbContext db)
{
    public async Task<DashboardDto> GetAsync()
    {
        // One query: SELECT Status, COUNT(*) FROM TaskItems GROUP BY Status.
        // One grouped query means all three numbers come from the same snapshot of the
        // data. At most one row per status comes back.
        var countsByStatus = await db.TaskItems
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        int CountOf(TaskItemStatus status) => countsByStatus.GetValueOrDefault(status);

        // Total is the sum of every status's count (all tasks). It equals
        // Completed + Pending because Todo, InProgress and Done are the only statuses.
        var total = countsByStatus.Values.Sum();
        var completed = CountOf(TaskItemStatus.Done);
        var pending = CountOf(TaskItemStatus.Todo) + CountOf(TaskItemStatus.InProgress);
        return new DashboardDto(total, completed, pending);
    }
}
