using System.ComponentModel.DataAnnotations;

namespace CommunicationService.Common.Messaging;

public sealed class CommunicationQueueOptions
{
    [Required]
    public string DeadLetterExchangeName { get; set; } = "medconnect.dlx";

    [Required]
    public string AppointmentCreatedQueue { get; set; } = "communication.appointment-created.q";

    [Required]
    public string ParticipantNameUpdatedQueue { get; set; } = "communication.participant-name-updated.q";

    [Range(1, ushort.MaxValue)]
    public ushort PrefetchCount { get; set; } = 1;
}
