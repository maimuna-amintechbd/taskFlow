using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Dtos;
using TaskFlow.Api.Services;

namespace TaskFlow.Api.Controllers;

/// <summary>
/// HTTP only: calls DashboardService and returns its totals. No counting and no
/// database access live here.
/// </summary>
[ApiController]
[Route("api/dashboard")]
public class DashboardController(DashboardService dashboard) : ControllerBase
{
    /// <summary>GET /api/dashboard — task totals across all projects, counted on every request.</summary>
    [HttpGet]
    [ProducesResponseType<DashboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardDto>> Get() => await dashboard.GetAsync();
}
