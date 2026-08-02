using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Domain.Entities;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public class RegisterPatientValidator : AbstractValidator<RegisterPatientDto>
{
    public RegisterPatientValidator()
    {
        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Фамилия обязательна.")
            .MaximumLength(Patient.MaxLastNameLength)
                .WithMessage($"Фамилия не должна превышать {Patient.MaxLastNameLength} символов.");

        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Имя обязательно.")
            .MaximumLength(Patient.MaxFirstNameLength)
                .WithMessage($"Имя не должно превышать {Patient.MaxFirstNameLength} символов.");

        RuleFor(x => x.MiddleName)
            .MaximumLength(Patient.MaxMiddleNameLength)
                .WithMessage($"Отчество не должно превышать {Patient.MaxMiddleNameLength} символов.")
            .When(x => !string.IsNullOrEmpty(x.MiddleName));

        RuleFor(x => x.Phone)
            .Matches(Patient.PhoneRegexPattern)
                .WithMessage("Некорректный формат номера телефона.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.DateOfBirth)
            .Cascade(CascadeMode.Stop)
            .LessThan(DateTime.UtcNow)
                .WithMessage("Дата рождения не может быть в будущем.")
            .GreaterThan(Patient.MinDateOfBirth)
                .WithMessage("Некорректная дата рождения.")
            .When(x => x.DateOfBirth.HasValue);
    }
}
