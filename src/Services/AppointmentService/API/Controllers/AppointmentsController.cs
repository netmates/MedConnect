using AppointmentService.API.Auth;
using AppointmentService.Application.DTOs.Appointment;
using AppointmentService.Application.Interfaces.Services;
using AppointmentService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.API.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController(IAppointmentApplicationService service) : ControllerBase
{
    private readonly IAppointmentApplicationService _service = service;

    /// <summary>GET /api/appointments/my — список записей текущего пациента.</summary>
    [HttpGet("my")]
    [Authorize(Roles = "patient")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetMyAppointments(
        [FromQuery] AppointmentStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var keycloakId = CurrentUser.GetKeycloakId(User);
        var result = await _service.GetByPatientAsync(keycloakId, status, from, to, ct);
        return Ok(result);
    }

    /// <summary>GET /api/appointments/doctor/my — список записей текущего врача.</summary>
    [HttpGet("doctor/my")]
    [Authorize(Roles = "doctor")]
    [ProducesResponseType(typeof(IReadOnlyList<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetMyDoctorAppointments(
        [FromQuery] AppointmentStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken ct)
    {
        var keycloakId = CurrentUser.GetKeycloakId(User);
        var result = await _service.GetByDoctorAsync(keycloakId, status, from, to, ct);
        return Ok(result);
    }

    /// <summary>GET /api/appointments/{id} — запись по id (пациент или врач этой записи).</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "patient,doctor")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> GetById(Guid id, CancellationToken ct)
    {
        var keycloakId = CurrentUser.GetKeycloakId(User);
        var result = await _service.GetByIdAsync(id, keycloakId, ct);
        return Ok(result);
    }

    /// <summary>POST /api/appointments — создать запись на приём.</summary>
    [HttpPost]
    [Authorize(Roles = "patient")]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDto>> Create([FromBody] CreateAppointmentDto dto, CancellationToken ct)
    {
        var keycloakId = CurrentUser.GetKeycloakId(User);
        var result = await _service.CreateAsync(dto, keycloakId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>POST /api/appointments/{id}/cancel — отменить запись (пациент или врач).</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "patient,doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var keycloakId = CurrentUser.GetKeycloakId(User);
        await _service.CancelAsync(id, keycloakId, ct);
        return NoContent();
    }

    /// <summary>POST /api/appointments/{id}/confirm — подтвердить запись (врач).</summary>
    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var keycloakId = CurrentUser.GetKeycloakId(User);
        await _service.ConfirmAsync(id, keycloakId, ct);
        return NoContent();
    }

    /// <summary>POST /api/appointments/{id}/complete — завершить приём (врач).</summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Complete(Guid id, CancellationToken ct)
    {
        var keycloakId = CurrentUser.GetKeycloakId(User);
        await _service.CompleteAsync(id, keycloakId, ct);
        return NoContent();
    }
}
