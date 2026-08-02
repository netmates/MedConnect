using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.API.Controllers;

[ApiController]
[Route("api/doctors")]
[Authorize]
public class DoctorsController(IDoctorApplicationService service) : ControllerBase
{
    private readonly IDoctorApplicationService _service = service;

    /// <summary>GET /api/doctors — список активных врачей, опционально с фильтром по специализации.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetAll([FromQuery] Guid? specializationId, CancellationToken ct)
    {
        var result = await _service.GetAllAsync(specializationId, ct);
        return Ok(result);
    }

    /// <summary>GET /api/doctors/{id} — профиль активного врача с его специализациями.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(result);
    }
}
