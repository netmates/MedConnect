using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.API.Controllers;

[ApiController]
[Route("api/patients")]
[Authorize(Roles = "patient")]
public class PatientsController(IPatientApplicationService service) : ControllerBase
{
    private readonly IPatientApplicationService _service = service;

    /// <summary>POST /api/patients/register — зарегистрировать или получить профиль пациента.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PatientDto>> Register([FromBody] RegisterPatientDto dto, CancellationToken ct)
    {
        var keycloakId = User.FindFirst("sub")!.Value;
        var result = await _service.RegisterOrGetAsync(keycloakId, dto, ct);
        return Ok(result);
    }

    /// <summary>GET /api/patients/me — профиль текущего пациента.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> GetMe(CancellationToken ct)
    {
        var keycloakId = User.FindFirst("sub")!.Value;
        var result = await _service.GetByKeycloakIdAsync(keycloakId, ct);
        return Ok(result);
    }

    /// <summary>PUT /api/patients/me — обновить профиль текущего пациента.</summary>
    [HttpPut("me")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> UpdateMe([FromBody] UpdatePatientDto dto, CancellationToken ct)
    {
        var keycloakId = User.FindFirst("sub")!.Value;
        var result = await _service.UpdateAsync(keycloakId, dto, ct);
        return Ok(result);
    }
}
