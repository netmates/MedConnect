using CommunicationService.Common.Persistence;

namespace CommunicationService.Features.Chats;

public sealed record CreateChatResponse(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    string PatientName,
    string DoctorName,
    DateTime CreatedAt)
{
    public static CreateChatResponse From(ChatDocument chat)
    {
        return new CreateChatResponse(
            chat.Id,
            chat.AppointmentId,
            chat.PatientId,
            chat.DoctorId,
            chat.PatientName,
            chat.DoctorName,
            chat.CreatedAt);
    }
}
