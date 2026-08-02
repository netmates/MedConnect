using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.DTOs.Specialization;
using AppointmentService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppointmentService.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController(
    ISpecializationApplicationService specializationService,
    IDoctorApplicationService doctorService,
    IAdminPatientApplicationService patientService) : ControllerBase
{
    private readonly ISpecializationApplicationService _specializationService = specializationService;
    private readonly IDoctorApplicationService _doctorService = doctorService;
    private readonly IAdminPatientApplicationService _patientService = patientService;

    // ── Специализации ────────────────────────────────────────────────────────

    /// <summary>GET /api/admin/specializations — список всех специализаций.</summary>
    [HttpGet("specializations")]
    [ProducesResponseType(typeof(IReadOnlyList<SpecializationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<SpecializationDto>>> GetSpecializations(CancellationToken ct)
    {
        var result = await _specializationService.GetAllAsync(ct);
        return Ok(result);
    }

    /// <summary>POST /api/admin/specializations — создать специализацию.</summary>
    [HttpPost("specializations")]
    [ProducesResponseType(typeof(SpecializationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SpecializationDto>> CreateSpecialization([FromBody] CreateSpecializationDto dto, CancellationToken ct)
    {
        var result = await _specializationService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetSpecializations), result);
    }

    /// <summary>PUT /api/admin/specializations/{id} — обновить специализацию.</summary>
    [HttpPut("specializations/{id:guid}")]
    [ProducesResponseType(typeof(SpecializationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpecializationDto>> UpdateSpecialization(Guid id, [FromBody] UpdateSpecializationDto dto, CancellationToken ct)
    {
        var result = await _specializationService.UpdateAsync(id, dto, ct);
        return Ok(result);
    }

    /// <summary>DELETE /api/admin/specializations/{id} — удалить специализацию.</summary>
    [HttpDelete("specializations/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteSpecialization(Guid id, CancellationToken ct)
    {
        await _specializationService.DeleteAsync(id, ct);
        return NoContent();
    }

    // ── Врачи ────────────────────────────────────────────────────────

    /// <summary>GET /api/admin/doctors — список всех врачей (включая неактивных).</summary>
    [HttpGet("doctors")]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetDoctors(CancellationToken ct)
    {
        var result = await _doctorService.GetAllIncludingInactiveAsync(ct);
        return Ok(result);
    }

    /// <summary>POST /api/admin/doctors — создать врача.</summary>
    [HttpPost("doctors")]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> CreateDoctor([FromBody] CreateDoctorDto dto, CancellationToken ct)
    {
        var result = await _doctorService.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(GetDoctors), result);
    }

    /// <summary>PUT /api/admin/doctors/{id} — обновить профиль врача.</summary>
    [HttpPut("doctors/{id:guid}")]
    [ProducesResponseType(typeof(DoctorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorDto>> UpdateDoctor(Guid id, [FromBody] UpdateDoctorDto dto, CancellationToken ct)
    {
        var result = await _doctorService.UpdateAsync(id, dto, ct);
        return Ok(result);
    }

    /// <summary>POST /api/admin/doctors/{id}/deactivate — деактивировать врача (soft-delete + Keycloak).</summary>
    [HttpPost("doctors/{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeactivateDoctor(Guid id, CancellationToken ct)
    {
        await _doctorService.DeactivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/admin/doctors/{id}/activate — восстановить врача (+ разблокировать в Keycloak).</summary>
    [HttpPost("doctors/{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ActivateDoctor(Guid id, CancellationToken ct)
    {
        await _doctorService.ActivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/admin/doctors/{id}/reset-password — сбросить пароль врача.</summary>
    [HttpPost("doctors/{id:guid}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ResetDoctorPassword(Guid id, [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        await _doctorService.ResetPasswordAsync(id, dto, ct);
        return NoContent();
    }

    // ── Пациенты ────────────────────────────────────────────────────────

    /// <summary>GET /api/admin/patients — список всех пациентов.</summary>
    [HttpGet("patients")]
    [ProducesResponseType(typeof(IReadOnlyList<PatientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<PatientDto>>> GetPatients(CancellationToken ct)
    {
        var result = await _patientService.GetAllIncludingInactiveAsync(ct);
        return Ok(result);
    }

    /// <summary>GET /api/admin/patients/{id} — профиль конкретного пациента.</summary>
    [HttpGet("patients/{id:guid}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> GetPatient(Guid id, CancellationToken ct)
    {
        var result = await _patientService.GetByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>PUT /api/admin/patients/{id} — редактировать профиль пациента.</summary>
    [HttpPut("patients/{id:guid}")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDto>> UpdatePatient(Guid id, [FromBody] UpdatePatientDto dto, CancellationToken ct)
    {
        var result = await _patientService.UpdateAsync(id, dto, ct);
        return Ok(result);
    }

    /// <summary>POST /api/admin/patients/{id}/deactivate — деактивировать пациента (soft-delete + Keycloak).</summary>
    [HttpPost("patients/{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeactivatePatient(Guid id, CancellationToken ct)
    {
        await _patientService.DeactivateAsync(id, ct);
        return NoContent();
    }

    /// <summary>POST /api/admin/patients/{id}/activate — восстановить пациента.</summary>
    [HttpPost("patients/{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ActivatePatient(Guid id, CancellationToken ct)
    {
        await _patientService.ActivateAsync(id, ct);
        return NoContent();
    }
}
