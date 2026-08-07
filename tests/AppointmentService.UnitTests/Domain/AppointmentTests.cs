using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using AppointmentService.Domain.Exceptions;

namespace AppointmentService.UnitTests.Domain;

public class AppointmentTests
{
    private static Appointment CreateValid(string? reason = "Осмотр")
        => Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), reason);

    [Fact]
    public void Create_WithValidData_SetsCreatedStatusAndTrimsReason()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var slotId = Guid.NewGuid();

        // Act
        var appointment = Appointment.Create(patientId, doctorId, slotId, "  жалоба  ");

        // Assert
        Assert.NotEqual(Guid.Empty, appointment.Id);
        Assert.Equal(patientId, appointment.PatientId);
        Assert.Equal(doctorId, appointment.DoctorId);
        Assert.Equal(slotId, appointment.SlotId);
        Assert.Equal("жалоба", appointment.Reason);
        Assert.Equal(AppointmentStatus.Created, appointment.Status);
    }

    [Fact]
    public void Create_WithWhitespaceReason_SetsReasonNull()
    {
        // Arrange
        // Act
        var appointment = CreateValid("   ");

        // Assert
        Assert.Null(appointment.Reason);
    }

    [Fact]
    public void Create_WithEmptyPatientId_Throws()
    {
        // Arrange
        // Act
        var ex = Assert.Throws<DomainException>(() =>
            Appointment.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), null));

        // Assert
        Assert.Equal("PatientId обязателен.", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyDoctorId_Throws()
    {
        // Arrange
        // Act
        var ex = Assert.Throws<DomainException>(() =>
            Appointment.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), null));

        // Assert
        Assert.Equal("DoctorId обязателен.", ex.Message);
    }

    [Fact]
    public void Create_WithEmptySlotId_Throws()
    {
        // Arrange
        // Act
        var ex = Assert.Throws<DomainException>(() =>
            Appointment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null));

        // Assert
        Assert.Equal("SlotId обязателен.", ex.Message);
    }

    [Fact]
    public void Create_WithReasonTooLong_Throws()
    {
        // Arrange
        var reason = new string('x', Appointment.MaxReasonLength + 1);

        // Act
        var ex = Assert.Throws<DomainException>(() => CreateValid(reason));

        // Assert
        Assert.Contains(Appointment.MaxReasonLength.ToString(), ex.Message);
    }

    [Fact]
    public void Confirm_FromCreated_SetsConfirmed()
    {
        // Arrange
        var appointment = CreateValid();

        // Act
        appointment.Confirm();

        // Assert
        Assert.Equal(AppointmentStatus.Confirmed, appointment.Status);
    }

    [Fact]
    public void Confirm_WhenNotCreated_Throws()
    {
        // Arrange
        var appointment = CreateValid();
        appointment.Confirm();

        // Act
        var ex = Assert.Throws<DomainException>(() => appointment.Confirm());

        // Assert
        Assert.Equal("Подтвердить можно только созданную запись.", ex.Message);
    }

    [Fact]
    public void Cancel_FromCreated_SetsCancelled()
    {
        // Arrange
        var appointment = CreateValid();

        // Act
        appointment.Cancel();

        // Assert
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
    }

    [Fact]
    public void Cancel_FromConfirmed_SetsCancelled()
    {
        // Arrange
        var appointment = CreateValid();
        appointment.Confirm();

        // Act
        appointment.Cancel();

        // Assert
        Assert.Equal(AppointmentStatus.Cancelled, appointment.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        // Arrange
        var appointment = CreateValid();
        appointment.Cancel();

        // Act + Assert
        Assert.Throws<DomainException>(() => appointment.Cancel());
    }

    [Fact]
    public void Cancel_WhenCompleted_Throws()
    {
        // Arrange
        var appointment = CreateValid();
        appointment.Confirm();
        appointment.Complete();

        // Act + Assert
        Assert.Throws<DomainException>(() => appointment.Cancel());
    }

    [Fact]
    public void Complete_FromConfirmed_SetsCompleted()
    {
        // Arrange
        var appointment = CreateValid();
        appointment.Confirm();

        // Act
        appointment.Complete();

        // Assert
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
    }

    [Fact]
    public void Complete_FromCreated_Throws()
    {
        // Arrange
        var appointment = CreateValid();

        // Act
        var ex = Assert.Throws<DomainException>(() => appointment.Complete());

        // Assert
        Assert.Equal("Завершить можно только подтверждённую запись. Текущий статус: Created.", ex.Message);
    }

    [Fact]
    public void Create_Confirm_Complete_HappyPath()
    {
        // Arrange
        var appointment = CreateValid();

        // Act
        appointment.Confirm();
        appointment.Complete();

        // Assert
        Assert.Equal(AppointmentStatus.Completed, appointment.Status);
    }
}
