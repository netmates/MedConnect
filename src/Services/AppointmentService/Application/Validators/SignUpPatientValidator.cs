using AppointmentService.Application.DTOs.Patient;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public sealed class SignUpPatientValidator : AbstractValidator<SignUpPatientDto>
{
    private const int MinPasswordLength = 8;

    public SignUpPatientValidator()
    {
        Include(new PatientProfileValidator<SignUpPatientDto>());

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Email обязателен.")
            .EmailAddress()
                .WithMessage("Некорректный email.");

        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Пароль обязателен.")
            .MinimumLength(MinPasswordLength)
                .WithMessage($"Пароль должен содержать минимум {MinPasswordLength} символов.");
    }
}
