using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.API.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
public sealed class DoctorsController(IDoctorApplicationService service) : ControllerBase
{
    /// <summary>GET /api/doctors — список активных врачей, опционально с фильтром по специализации.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetAll([FromQuery] Guid? specializationId, CancellationToken ct)
    {
        var result = await service.GetAllAsync(specializationId, ct);
        return Ok(result);
    }
}
