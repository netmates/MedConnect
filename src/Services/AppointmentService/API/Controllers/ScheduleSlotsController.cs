using AppointmentService.API.Auth;
using AppointmentService.Application.Auth;
using AppointmentService.Application.DTOs.ScheduleSlot;
using AppointmentService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AppointmentService.API.Controllers;

[ApiController]
[Route("api/slots")]
public class ScheduleSlotsController(IScheduleSlotApplicationService service) : ControllerBase
{
    private readonly IScheduleSlotApplicationService _service = service;

    /// <summary>GET /api/slots — полное расписание текущего врача.</summary>
    [HttpGet]
    [Authorize(Roles = Roles.Doctor)]
    [ProducesResponseType(typeof(IReadOnlyList<ScheduleSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ScheduleSlotDto>>> GetSchedule(CancellationToken ct)
    {
        var keycloakId = CurrentUser.GetKeycloakId(User);
        var result = await _service.GetScheduleAsync(keycloakId, ct);
        return Ok(result);
    }

    /// <summary>GET /api/slots/available — доступные слоты врача на указанную дату.</summary>
    [HttpGet("available")]
    [Authorize(Roles = Roles.PatientOrDoctor)]
    [ProducesResponseType(typeof(IReadOnlyList<ScheduleSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ScheduleSlotDto>>> GetAvailable(
        [FromQuery, BindRequired] Guid doctorId,
        [FromQuery, BindRequired] DateTime date,
        CancellationToken ct)
    {
        var result = await _service.GetAvailableAsync(doctorId, date, ct);
        return Ok(result);
    }

    /// <summary>POST /api/slots — создать слот расписания (врач).</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Doctor)]
    [ProducesResponseType(typeof(ScheduleSlotDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduleSlotDto>> Create([FromBody] CreateScheduleSlotDto dto, CancellationToken ct)
    {
        var keycloakId = CurrentUser.GetKeycloakId(User);
        var result = await _service.CreateAsync(dto, keycloakId, ct);
        return CreatedAtAction(nameof(GetSchedule), result);
    }

    /// <summary>PUT /api/slots/{id} — обновить слот расписания (врач).</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Doctor)]
    [ProducesResponseType(typeof(ScheduleSlotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ScheduleSlotDto>> Update(Guid id, [FromBody] UpdateScheduleSlotDto dto, CancellationToken ct)
    {
        var keycloakId = CurrentUser.GetKeycloakId(User);
        var result = await _service.UpdateAsync(id, dto, keycloakId, ct);
        return Ok(result);
    }

    /// <summary>DELETE /api/slots/{id} — удалить слот расписания (врач).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Doctor)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        var keycloakId = CurrentUser.GetKeycloakId(User);
        await _service.DeleteAsync(id, keycloakId, ct);
        return NoContent();
    }
}
