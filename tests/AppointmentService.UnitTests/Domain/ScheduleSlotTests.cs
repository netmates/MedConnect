using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Enums;
using AppointmentService.Domain.Exceptions;

namespace AppointmentService.UnitTests.Domain;

public class ScheduleSlotTests
{
    private static readonly DateTime Start = new(2030, 1, 10, 10, 0, 0, DateTimeKind.Utc);

    private static ScheduleSlot CreateValid(
        Guid? doctorId = null,
        DateTime? start = null,
        DateTime? end = null)
        => ScheduleSlot.Create(
            doctorId ?? Guid.NewGuid(),
            start ?? Start,
            end ?? Start.AddMinutes(30));

    [Fact]
    public void Create_WithValidData_SetsAvailableStatusAndFields()
    {
        // Arrange
        var doctorId = Guid.NewGuid();
        var end = Start.AddMinutes(30);

        // Act
        var slot = ScheduleSlot.Create(doctorId, Start, end);

        // Assert
        Assert.NotEqual(Guid.Empty, slot.Id);
        Assert.Equal(doctorId, slot.DoctorId);
        Assert.Equal(Start, slot.StartTime);
        Assert.Equal(end, slot.EndTime);
        Assert.Equal(SlotStatus.Available, slot.Status);
    }

    [Fact]
    public void Create_WithEmptyDoctorId_Throws()
    {
        // Arrange
        // Act
        var ex = Assert.Throws<DomainException>(() =>
            ScheduleSlot.Create(Guid.Empty, Start, Start.AddMinutes(30)));

        // Assert
        Assert.Equal("DoctorId обязателен.", ex.Message);
    }

    [Fact]
    public void Create_WhenEndEqualsStart_Throws()
    {
        // Arrange
        // Act
        var ex = Assert.Throws<DomainException>(() =>
            ScheduleSlot.Create(Guid.NewGuid(), Start, Start));

        // Assert
        Assert.Equal("Конец слота должен быть позже начала.", ex.Message);
    }

    [Fact]
    public void Create_WhenEndBeforeStart_Throws()
    {
        // Arrange
        // Act
        var ex = Assert.Throws<DomainException>(() =>
            ScheduleSlot.Create(Guid.NewGuid(), Start, Start.AddMinutes(-1)));

        // Assert
        Assert.Equal("Конец слота должен быть позже начала.", ex.Message);
    }

    [Fact]
    public void Create_WithDurationTooShort_Throws()
    {
        // Arrange
        var end = Start.AddMinutes(ScheduleSlot.MinDurationMinutes - 1);

        // Act
        var ex = Assert.Throws<DomainException>(() =>
            ScheduleSlot.Create(Guid.NewGuid(), Start, end));

        // Assert
        Assert.Equal(
            $"Длительность слота должна быть от {ScheduleSlot.MinDurationMinutes} до {ScheduleSlot.MaxDurationMinutes} минут.",
            ex.Message);
    }

    [Fact]
    public void Create_WithDurationTooLong_Throws()
    {
        // Arrange
        var end = Start.AddMinutes(ScheduleSlot.MaxDurationMinutes + 1);

        // Act
        var ex = Assert.Throws<DomainException>(() =>
            ScheduleSlot.Create(Guid.NewGuid(), Start, end));

        // Assert
        Assert.Equal(
            $"Длительность слота должна быть от {ScheduleSlot.MinDurationMinutes} до {ScheduleSlot.MaxDurationMinutes} минут.",
            ex.Message);
    }

    [Fact]
    public void Create_WithMinDuration_Succeeds()
    {
        // Arrange
        var end = Start.AddMinutes(ScheduleSlot.MinDurationMinutes);

        // Act
        var slot = ScheduleSlot.Create(Guid.NewGuid(), Start, end);

        // Assert
        Assert.Equal(SlotStatus.Available, slot.Status);
        Assert.Equal(end, slot.EndTime);
    }

    [Fact]
    public void Create_WithMaxDuration_Succeeds()
    {
        // Arrange
        var end = Start.AddMinutes(ScheduleSlot.MaxDurationMinutes);

        // Act
        var slot = ScheduleSlot.Create(Guid.NewGuid(), Start, end);

        // Assert
        Assert.Equal(SlotStatus.Available, slot.Status);
        Assert.Equal(end, slot.EndTime);
    }

    [Fact]
    public void Book_FromAvailable_SetsBooked()
    {
        // Arrange
        var slot = CreateValid();

        // Act
        slot.Book();

        // Assert
        Assert.Equal(SlotStatus.Booked, slot.Status);
    }

    [Fact]
    public void Book_WhenAlreadyBooked_Throws()
    {
        // Arrange
        var slot = CreateValid();
        slot.Book();

        // Act
        var ex = Assert.Throws<DomainException>(() => slot.Book());

        // Assert
        Assert.Equal("Слот недоступен для бронирования.", ex.Message);
    }

    [Fact]
    public void Book_WhenConsumed_Throws()
    {
        // Arrange
        var slot = CreateValid();
        slot.Book();
        slot.Consume();

        // Act
        var ex = Assert.Throws<DomainException>(() => slot.Book());

        // Assert
        Assert.Equal("Слот недоступен для бронирования.", ex.Message);
    }

    [Fact]
    public void Free_FromBooked_SetsAvailable()
    {
        // Arrange
        var slot = CreateValid();
        slot.Book();

        // Act
        slot.Free();

        // Assert
        Assert.Equal(SlotStatus.Available, slot.Status);
    }

    [Fact]
    public void Free_WhenAvailable_Throws()
    {
        // Arrange
        var slot = CreateValid();

        // Act
        var ex = Assert.Throws<DomainException>(() => slot.Free());

        // Assert
        Assert.Equal("Освободить можно только забронированный слот.", ex.Message);
    }

    [Fact]
    public void Free_WhenConsumed_Throws()
    {
        // Arrange
        var slot = CreateValid();
        slot.Book();
        slot.Consume();

        // Act
        var ex = Assert.Throws<DomainException>(() => slot.Free());

        // Assert
        Assert.Equal("Освободить можно только забронированный слот.", ex.Message);
    }

    [Fact]
    public void Consume_FromBooked_SetsConsumed()
    {
        // Arrange
        var slot = CreateValid();
        slot.Book();

        // Act
        slot.Consume();

        // Assert
        Assert.Equal(SlotStatus.Consumed, slot.Status);
    }

    [Fact]
    public void Consume_WhenAvailable_Throws()
    {
        // Arrange
        var slot = CreateValid();

        // Act
        var ex = Assert.Throws<DomainException>(() => slot.Consume());

        // Assert
        Assert.Equal("Отметить использованным можно только забронированный слот.", ex.Message);
    }

    [Fact]
    public void Update_WhenAvailable_ChangesTimes()
    {
        // Arrange
        var slot = CreateValid();
        var newStart = Start.AddHours(1);
        var newEnd = newStart.AddMinutes(45);

        // Act
        slot.Update(newStart, newEnd);

        // Assert
        Assert.Equal(newStart, slot.StartTime);
        Assert.Equal(newEnd, slot.EndTime);
        Assert.Equal(SlotStatus.Available, slot.Status);
    }

    [Fact]
    public void Update_WhenBooked_Throws()
    {
        // Arrange
        var slot = CreateValid();
        slot.Book();
        var newStart = Start.AddHours(2);
        var newEnd = newStart.AddMinutes(30);

        // Act
        var ex = Assert.Throws<DomainException>(() => slot.Update(newStart, newEnd));

        // Assert
        Assert.Equal("Редактировать можно только свободный слот.", ex.Message);
    }

    [Fact]
    public void Update_WhenAvailable_WithInvalidDuration_Throws()
    {
        // Arrange
        var slot = CreateValid();
        var newEnd = Start.AddMinutes(ScheduleSlot.MinDurationMinutes - 1);

        // Act
        var ex = Assert.Throws<DomainException>(() => slot.Update(Start, newEnd));

        // Assert
        Assert.Equal(
            $"Длительность слота должна быть от {ScheduleSlot.MinDurationMinutes} до {ScheduleSlot.MaxDurationMinutes} минут.",
            ex.Message);
    }

    [Fact]
    public void Book_Free_Book_HappyPath()
    {
        // Arrange
        var slot = CreateValid();

        // Act
        slot.Book();
        slot.Free();
        slot.Book();

        // Assert
        Assert.Equal(SlotStatus.Booked, slot.Status);
    }
}
