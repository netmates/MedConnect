using AppointmentService.Application.DTOs.Patient;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public sealed class RegisterPatientValidator : AbstractValidator<RegisterPatientDto>
{
    public RegisterPatientValidator() =>
        Include(new PatientProfileValidator<RegisterPatientDto>());
}
