using FluentValidation;

namespace CommunicationService.Features.Messages;

public sealed class SendMessageValidator : AbstractValidator<SendMessageRequest>
{
    public const int MaxTextLength = 2000;

    public SendMessageValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(MaxTextLength);
    }
}
