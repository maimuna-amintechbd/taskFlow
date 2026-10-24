using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

/// <summary>
/// HTTP only: binds input, calls one ProjectService method and maps its result
/// to a status code. No rules and no database access live here.
/// </summary>
[ApiController]
[Route("api/projects")]
public class ProjectsController(ProjectService projects) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> List() => await projects.ListAsync();

    [HttpGet("{id:int}")]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetById(int id)
    {
        var result = await projects.GetByIdAsync(id);
        return result.Kind switch
        {
            ResultKind.Ok => Ok(result.Value),
            ResultKind.NotFound => NotFound(),
            _ => throw new InvalidOperationException($"Unexpected result kind {result.Kind}."),
        };
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProjectDto>> Create(SaveProjectRequest request)
    {
        var result = await projects.CreateAsync(request);
        return result.Kind switch
        {
            ResultKind.Ok => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value),
            ResultKind.Invalid => ValidationProblem(new ValidationProblemDetails(result.Errors)),
            _ => throw new InvalidOperationException($"Unexpected result kind {result.Kind}."),
        };
    }
}
