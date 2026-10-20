using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Domain.Entities;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public sealed class UpdatePatientValidator : AbstractValidator<UpdatePatientDto>
{
    public UpdatePatientValidator()
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
            .Cascade(CascadeMode.Stop)
            .MaximumLength(Patient.MaxPhoneLength)
                .WithMessage($"Телефон не должен превышать {Patient.MaxPhoneLength} символов.")
            .Matches(Patient.PhoneRegexPattern)
                .WithMessage("Некорректный формат номера телефона.")
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.DateOfBirth)
            .Cascade(CascadeMode.Stop)
            .Must(date => date!.Value.Date >= Patient.MinDateOfBirth.Date)
                .WithMessage($"Дата рождения не может быть раньше {Patient.MinDateOfBirth:yyyy-MM-dd}.")
            .Must(date => date!.Value.Date < DateTime.UtcNow.Date)
                .WithMessage("Дата рождения должна быть в прошлом.")
            .When(x => x.DateOfBirth.HasValue);
    }
}
