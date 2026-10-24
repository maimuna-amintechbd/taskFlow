using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Data;

/// <summary>
/// The EF Core "unit of work": one instance per HTTP request (scoped), tracking
/// the entities loaded or added during that request and saving them together.
/// </summary>
public class TaskFlowDbContext(DbContextOptions<TaskFlowDbContext> options) : DbContext(options)
{
    // Each DbSet becomes a table; queries on it are translated to SQL.
    public DbSet<Project> Projects => Set<Project>();
}
