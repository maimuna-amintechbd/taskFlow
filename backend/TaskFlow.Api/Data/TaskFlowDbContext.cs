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

    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    /// <summary>
    /// Fluent configuration for what conventions can't infer. It is read once when
    /// the model is built and is also what migrations are generated from.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // One project has many tasks; every task needs a project. Cascade puts
        // ON DELETE CASCADE on the foreign key, so the database itself removes a
        // project's tasks when the project row is deleted.
        modelBuilder.Entity<Project>()
            .HasMany(p => p.Tasks)
            .WithOne(t => t.Project)
            .HasForeignKey(t => t.ProjectId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        var task = modelBuilder.Entity<TaskItem>();

        // Store enum names ("InProgress") rather than numbers, so the data stays
        // readable and reordering the enum can't silently change saved values.
        task.Property(t => t.Status).HasConversion<string>();
        task.Property(t => t.Priority).HasConversion<string>();

        // SQLite has no date-time-with-zone type, so EF Core reads DateTime back with
        // Kind = Unspecified. We always write UTC, so mark it as UTC on the way out;
        // the JSON then ends in "Z".
        task.Property(t => t.CreatedAt)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }
}
