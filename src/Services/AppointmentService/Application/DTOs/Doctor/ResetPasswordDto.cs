namespace AppointmentService.Application.DTOs.Doctor;

public sealed record ResetPasswordDto
{
    public string NewPassword { get; init; } = string.Empty;
}
