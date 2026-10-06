using CommunicationService.Common.Persistence;
using CommunicationService.Features;

namespace CommunicationService.UnitTests.Persistence;

public class DocumentTests
{
    [Fact]
    public void MessageDocument_Create_TrimsText()
    {
        // Arrange
        var chatId = Guid.NewGuid();

        // Act
        var message = MessageDocument.Create(chatId, "patient-1", "patient", "  Здравствуйте  ");

        // Assert
        Assert.NotEqual(Guid.Empty, message.Id);
        Assert.Equal(chatId, message.ChatId);
        Assert.Equal("patient-1", message.SenderId);
        Assert.Equal("patient", message.SenderRole);
        Assert.Equal("Здравствуйте", message.Text);
        Assert.Equal(DateTimeKind.Utc, message.CreatedAt.Kind);
    }

    [Fact]
    public void ChatDocument_Create_TrimsNames()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        // Act
        var chat = ChatDocument.Create(
            appointmentId,
            patientId,
            doctorId,
            "patient-1",
            "doctor-1",
            "  Иванов Иван  ",
            "  Петров Петр  ");

        // Assert
        Assert.NotEqual(Guid.Empty, chat.Id);
        Assert.Equal(appointmentId, chat.AppointmentId);
        Assert.Equal(patientId, chat.PatientId);
        Assert.Equal(doctorId, chat.DoctorId);
        Assert.Equal("patient-1", chat.PatientKeycloakId);
        Assert.Equal("doctor-1", chat.DoctorKeycloakId);
        Assert.Equal("Иванов Иван", chat.PatientName);
        Assert.Equal("Петров Петр", chat.DoctorName);
        Assert.Equal(DateTimeKind.Utc, chat.CreatedAt.Kind);
    }

    [Fact]
    public void MessageResponse_From_CopiesDocument()
    {
        // Arrange
        var message = MessageDocument.Create(Guid.NewGuid(), "patient-1", "patient", "Здравствуйте");

        // Act
        var response = MessageResponse.From(message);

        // Assert
        Assert.Equal(message.Id, response.Id);
        Assert.Equal(message.ChatId, response.ChatId);
        Assert.Equal(message.SenderId, response.SenderId);
        Assert.Equal(message.SenderRole, response.SenderRole);
        Assert.Equal(message.Text, response.Text);
        Assert.Equal(message.CreatedAt, response.CreatedAt);
    }
}
