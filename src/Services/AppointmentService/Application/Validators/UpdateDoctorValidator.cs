using AppointmentService.Application.DTOs.Doctor;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public sealed class UpdateDoctorValidator : AbstractValidator<UpdateDoctorDto>
{
    public UpdateDoctorValidator() =>
        Include(new DoctorProfileValidator<UpdateDoctorDto>());
}
