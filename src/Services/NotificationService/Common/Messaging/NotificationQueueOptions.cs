using System.ComponentModel.DataAnnotations;
using MedConnect.Shared.Consuming;

namespace NotificationService.Common.Messaging;

public sealed class NotificationQueueOptions : IConsumerQueueSettings
{
    [Required]
    public string DeadLetterExchangeName { get; set; } = "medconnect.dlx";

    [Required]
    public string AppointmentCreatedQueue { get; set; } = "notification.appointment-created.q";

    [Required]
    public string AppointmentCancelledQueue { get; set; } = "notification.appointment-cancelled.q";

    [Required]
    public string MessageCreatedQueue { get; set; } = "notification.message-created.q";

    [Range(1, ushort.MaxValue)]
    public ushort PrefetchCount { get; set; } = 10;
}
