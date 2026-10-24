using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Api.Data;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Models;

namespace TaskFlow.Api.Services;

/// <summary>
/// Owns the project rules (trimming, validation) and every EF Core query on projects.
/// Registered as scoped, so it shares the request's TaskFlowDbContext.
/// </summary>
public class ProjectService(TaskFlowDbContext db)
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 1000;

    /// <summary>
    /// The one place a Project becomes a ProjectDto. It is an expression tree, not a
    /// compiled method, so EF Core translates it into the SQL SELECT list.
    /// TaskCount is 0 until Story 3.1 adds tasks.
    /// </summary>
    private static readonly Expression<Func<Project, ProjectDto>> ToDto =
        p => new ProjectDto(p.Id, p.Name, p.Description, 0);

    public async Task<List<ProjectDto>> ListAsync() =>
        await db.Projects
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Select(ToDto)
            .ToListAsync();

    public async Task<ServiceResult<ProjectDto>> GetByIdAsync(int id)
    {
        var dto = await FindDtoAsync(id);
        return dto is null ? ServiceResult<ProjectDto>.NotFound() : ServiceResult<ProjectDto>.Ok(dto);
    }

    public async Task<ServiceResult<ProjectDto>> CreateAsync(SaveProjectRequest request)
    {
        var project = new Project();
        var errors = ValidateAndApply(request, project);
        if (errors.Count > 0)
        {
            return ServiceResult<ProjectDto>.Invalid(errors);
        }

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Re-read through the same projection so every response has one shape.
        var dto = await FindDtoAsync(project.Id);
        return ServiceResult<ProjectDto>.Ok(dto!);
    }

    /// <summary>
    /// Full replace: every field comes from the request, so an omitted description is cleared.
    /// An unknown id is reported before this service validates the body. Bodies the
    /// framework cannot read (empty, malformed, wrong types) are rejected earlier with 400.
    /// </summary>
    public async Task<ServiceResult<ProjectDto>> UpdateAsync(int id, SaveProjectRequest request)
    {
        // FindAsync returns a tracked entity, so SaveChangesAsync writes only what changed.
        var project = await db.Projects.FindAsync(id);
        if (project is null)
        {
            return ServiceResult<ProjectDto>.NotFound();
        }

        var errors = ValidateAndApply(request, project);
        if (errors.Count > 0)
        {
            // ValidateAndApply left the entity untouched, and nothing is saved.
            return ServiceResult<ProjectDto>.Invalid(errors);
        }

        await db.SaveChangesAsync();

        var dto = await FindDtoAsync(id);
        return ServiceResult<ProjectDto>.Ok(dto!);
    }

    /// <summary>
    /// Removes only the project row. Tasks are never loaded: from Story 3.1 the
    /// database's ON DELETE CASCADE removes them.
    /// </summary>
    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var project = await db.Projects.FindAsync(id);
        if (project is null)
        {
            return ServiceResult.NotFound();
        }

        db.Projects.Remove(project);
        await db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    private Task<ProjectDto?> FindDtoAsync(int id) =>
        db.Projects
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(ToDto)
            .SingleOrDefaultAsync();

    /// <summary>
    /// Checks every rule in one pass (at most one message per field) and copies the
    /// trimmed values onto <paramref name="project"/> only when there are no errors.
    /// Shared by create and update.
    /// </summary>
    private static Dictionary<string, string[]> ValidateAndApply(SaveProjectRequest request, Project project)
    {
        var errors = new Dictionary<string, string[]>();

        var name = request.Name?.Trim() ?? "";
        if (name.Length == 0)
        {
            errors["name"] = ["Name is required."];
        }
        else if (name.Length > NameMaxLength)
        {
            errors["name"] = [$"Name must be at most {NameMaxLength} characters."];
        }

        // A whitespace-only description is stored as null.
        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (description is not null && description.Length > DescriptionMaxLength)
        {
            errors["description"] = [$"Description must be at most {DescriptionMaxLength} characters."];
        }

        if (errors.Count == 0)
        {
            project.Name = name;
            project.Description = description;
        }

        return errors;
    }
}
