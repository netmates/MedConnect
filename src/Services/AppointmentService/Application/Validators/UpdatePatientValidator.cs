using AppointmentService.Application.DTOs.Patient;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public sealed class UpdatePatientValidator : AbstractValidator<UpdatePatientDto>
{
    public UpdatePatientValidator() =>
        Include(new PatientProfileValidator<UpdatePatientDto>());
}
