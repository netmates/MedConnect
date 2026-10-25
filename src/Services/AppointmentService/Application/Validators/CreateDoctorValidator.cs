using AppointmentService.Application.DTOs.Doctor;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public sealed class CreateDoctorValidator : AbstractValidator<CreateDoctorDto>
{
    public CreateDoctorValidator()
    {
        Include(new DoctorProfileValidator<CreateDoctorDto>());

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Email обязателен.")
            .EmailAddress()
                .WithMessage("Некорректный email.");

        RuleFor(x => x.TemporaryPassword)
            .NotEmpty()
            .WithMessage("Временный пароль обязателен.");
    }
}
