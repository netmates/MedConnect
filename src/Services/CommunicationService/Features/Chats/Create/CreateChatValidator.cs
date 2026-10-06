using FluentValidation;

namespace CommunicationService.Features.Chats.Create;

public sealed class CreateChatValidator : AbstractValidator<CreateChatRequest>
{
    public CreateChatValidator()
    {
        RuleFor(x => x.AppointmentId)
            .NotEmpty()
            .WithMessage("AppointmentId обязателен.");
    }
}
