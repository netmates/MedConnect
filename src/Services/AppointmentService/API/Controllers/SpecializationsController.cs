using AppointmentService.Application.DTOs.Specialization;
using AppointmentService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.API.Controllers;

[ApiController]
[Route("api/specializations")]
[Authorize]
public class SpecializationsController(ISpecializationApplicationService service) : ControllerBase
{
    private readonly ISpecializationApplicationService _service = service;

    /// <summary>GET /api/specializations — список всех специализаций (для поиска врача).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SpecializationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SpecializationDto>>> GetAll(CancellationToken ct)
    {
        var result = await _service.GetAllAsync(ct);
        return Ok(result);
    }
}
