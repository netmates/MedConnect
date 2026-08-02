using AppointmentService.Application.DTOs.Doctor;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public class ResetPasswordValidator : AbstractValidator<ResetPasswordDto>
{
    private const int MinPasswordLength = 8;

    public ResetPasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Новый пароль обязателен.")
            .MinimumLength(MinPasswordLength)
                .WithMessage($"Новый пароль должен содержать минимум {MinPasswordLength} символов.");
    }
}
