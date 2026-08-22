using FluentValidation;

namespace CommunicationService.Features.Chats;

public sealed class CreateChatValidator : AbstractValidator<CreateChatRequest>
{
    public const int MaxNameLength = 300;
    public const int MaxKeycloakIdLength = 255;

    public CreateChatValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty()
            .WithMessage("AppointmentId обязателен.");

        RuleFor(x => x.PatientId)
            .NotEmpty()
            .WithMessage("PatientId обязателен.");

        RuleFor(x => x.DoctorId)
            .NotEmpty()
            .WithMessage("DoctorId обязателен.");

        RuleFor(x => x.PatientKeycloakId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("PatientKeycloakId обязателен.")
            .MaximumLength(MaxKeycloakIdLength)
                .WithMessage($"PatientKeycloakId не должен превышать {MaxKeycloakIdLength} символов.");

        RuleFor(x => x.DoctorKeycloakId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("DoctorKeycloakId обязателен.")
            .MaximumLength(MaxKeycloakIdLength)
                .WithMessage($"DoctorKeycloakId не должен превышать {MaxKeycloakIdLength} символов.");

        RuleFor(x => x.PatientName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Имя пациента обязательно.")
            .MaximumLength(MaxNameLength)
                .WithMessage($"Имя пациента не должно превышать {MaxNameLength} символов.");

        RuleFor(x => x.DoctorName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Имя врача обязательно.")
            .MaximumLength(MaxNameLength)
                .WithMessage($"Имя врача не должно превышать {MaxNameLength} символов.");

        RuleFor(x => x)
            .Must(x => x.PatientKeycloakId != x.DoctorKeycloakId)
            .WithMessage("PatientKeycloakId и DoctorKeycloakId не должны совпадать.");
    }
}
