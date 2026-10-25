using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

/// <summary>
/// HTTP only: binds input, calls one TaskService method and maps its result
/// to a status code. No rules and no database access live here.
/// </summary>
[ApiController]
[Route("api/tasks")]
public class TasksController(TaskService tasks) : ControllerBase
{
    /// <summary>GET /api/tasks?projectId= — newest first; an invalid projectId is a 400.</summary>
    [HttpGet]
    [ProducesResponseType<List<TaskDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<TaskDto>>> List([FromQuery] TaskListQuery query)
    {
        var result = await tasks.ListAsync(query);
        return result.Kind switch
        {
            ResultKind.Ok => Ok(result.Value),
            ResultKind.Invalid => ValidationProblem(new ValidationProblemDetails(result.Errors)),
            _ => throw new InvalidOperationException($"Unexpected result kind {result.Kind}."),
        };
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<TaskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetById(int id)
    {
        var result = await tasks.GetByIdAsync(id);
        return result.Kind switch
        {
            ResultKind.Ok => Ok(result.Value),
            ResultKind.NotFound => NotFound(),
            _ => throw new InvalidOperationException($"Unexpected result kind {result.Kind}."),
        };
    }

    [HttpPost]
    [ProducesResponseType<TaskDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Create(SaveTaskRequest request)
    {
        var result = await tasks.CreateAsync(request);
        return result.Kind switch
        {
            ResultKind.Ok => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value),
            ResultKind.Invalid => ValidationProblem(new ValidationProblemDetails(result.Errors)),
            _ => throw new InvalidOperationException($"Unexpected result kind {result.Kind}."),
        };
    }

    /// <summary>
    /// PUT /api/tasks/{id} — full replace. An unknown id is a 404 even when the body breaks
    /// TaskService's rules; bodies the framework cannot read are rejected earlier with 400.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<TaskDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> Update(int id, SaveTaskRequest request)
    {
        var result = await tasks.UpdateAsync(id, request);
        return result.Kind switch
        {
            ResultKind.Ok => Ok(result.Value),
            ResultKind.NotFound => NotFound(),
            ResultKind.Invalid => ValidationProblem(new ValidationProblemDetails(result.Errors)),
            _ => throw new InvalidOperationException($"Unexpected result kind {result.Kind}."),
        };
    }
}
