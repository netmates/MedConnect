using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Domain.Entities;
using FluentValidation;

namespace AppointmentService.Application.Validators;

public class CreateDoctorValidator : AbstractValidator<CreateDoctorDto>
{
    public CreateDoctorValidator()
    {
        RuleFor(x => x.LastName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Фамилия обязательна.")
            .MaximumLength(Doctor.MaxLastNameLength)
                .WithMessage($"Фамилия не должна превышать {Doctor.MaxLastNameLength} символов.");

        RuleFor(x => x.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Имя обязательно.")
            .MaximumLength(Doctor.MaxFirstNameLength)
                .WithMessage($"Имя не должно превышать {Doctor.MaxFirstNameLength} символов.");

        RuleFor(x => x.MiddleName)
            .MaximumLength(Doctor.MaxMiddleNameLength)
                .WithMessage($"Отчество не должно превышать {Doctor.MaxMiddleNameLength} символов.")
            .When(x => !string.IsNullOrEmpty(x.MiddleName));

        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Email обязателен.")
            .EmailAddress()
                .WithMessage("Некорректный email.");

        RuleFor(x => x.TemporaryPassword)
            .NotEmpty()
            .WithMessage("Временный пароль обязателен.");

        RuleFor(x => x.ExperienceYears)
            .GreaterThanOrEqualTo(Doctor.MinExperienceYears)
                .WithMessage($"Опыт не может быть меньше {Doctor.MinExperienceYears} лет.")
            .LessThanOrEqualTo(Doctor.MaxExperienceYears)
                .WithMessage($"Опыт не может быть больше {Doctor.MaxExperienceYears} лет.");

        RuleFor(x => x.Description)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Описание обязательно.")
            .MaximumLength(Doctor.MaxDescriptionLength)
                .WithMessage($"Описание не должно превышать {Doctor.MaxDescriptionLength} символов.");

        RuleFor(x => x.SpecializationIds)
            .NotEmpty()
            .WithMessage("Укажите хотя бы одну специализацию.");
    }
}
