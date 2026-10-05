using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NotificationService.Features.Notifications;

public sealed class ConfiguredNotificationSender(
    IOptions<NotificationOptions> options,
    [FromKeyedServices(NotificationChannels.Fake)] INotificationSender fake,
    [FromKeyedServices(NotificationChannels.Email)] INotificationSender email,
    [FromKeyedServices(NotificationChannels.Sms)] INotificationSender sms) : INotificationSender
{
    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken)
    {
        var sender = options.Value.Channel switch
        {
            NotificationChannels.Email => email,
            NotificationChannels.Sms => sms,
            NotificationChannels.Fake => fake,
            _ => throw new InvalidOperationException(
                $"Unsupported notification channel '{options.Value.Channel}'.")
        };

        return sender.SendAsync(message, cancellationToken);
    }
}
