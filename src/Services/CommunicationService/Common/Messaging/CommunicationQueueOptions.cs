using System.ComponentModel.DataAnnotations;

namespace CommunicationService.Common.Messaging;

public sealed class CommunicationQueueOptions
{
    [Required]
    public string AppointmentCreatedQueue { get; set; } = "communication.appointment-created.q";

    [Required]
    public string ParticipantNameUpdatedQueue { get; set; } = "communication.participant-name-updated.q";
}
