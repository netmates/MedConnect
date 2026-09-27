using System.ComponentModel.DataAnnotations;

namespace NotificationService.Features.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    [Required]
    [AllowedValues(NotificationChannels.Fake, NotificationChannels.Email, NotificationChannels.Sms)]
    public string Channel { get; set; } = NotificationChannels.Fake;
}
