using FluentValidation;

namespace CommunicationService.Features.Chats;

public sealed class CreateChatValidator : AbstractValidator<CreateChatRequest>
{
    public const int MaxNameLength = 300;
    public const int MaxKeycloakIdLength = 255;

    public CreateChatValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();

        RuleFor(x => x.PatientKeycloakId).NotEmpty().MaximumLength(MaxKeycloakIdLength);
        RuleFor(x => x.DoctorKeycloakId).NotEmpty().MaximumLength(MaxKeycloakIdLength);

        RuleFor(x => x.PatientName).NotEmpty().MaximumLength(MaxNameLength);
        RuleFor(x => x.DoctorName).NotEmpty().MaximumLength(MaxNameLength);

        RuleFor(x => x)
            .Must(x => x.PatientKeycloakId != x.DoctorKeycloakId)
            .WithMessage("PatientKeycloakId и DoctorKeycloakId не должны совпадать.");
    }
}
