using AppointmentService.Application.DTOs.ScheduleSlot;
using AppointmentService.Application.Validators;
using AppointmentService.Domain.Entities;
using FluentValidation.Results;

namespace AppointmentService.UnitTests.Validators;

public class UpdateScheduleSlotValidatorTests
{
    private readonly UpdateScheduleSlotValidator _validator = new();

    private static readonly DateTime FutureStart =
        new(2030, 6, 15, 10, 0, 0, DateTimeKind.Utc);

    private static UpdateScheduleSlotDto ValidDto(
        DateTime? start = null,
        DateTime? end = null)
    {
        var startTime = start ?? FutureStart;
        return new UpdateScheduleSlotDto
        {
            StartTime = startTime,
            EndTime = end ?? startTime.AddMinutes(30)
        };
    }

    [Fact]
    public void Validate_WithValidDto_Passes()
    {
        // Arrange
        var dto = ValidDto();

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithStartTimeInPast_Fails()
    {
        // Arrange
        var past = DateTime.UtcNow.AddHours(-1);
        var dto = ValidDto(start: past, end: past.AddMinutes(30));

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateScheduleSlotDto.StartTime)
                 && e.ErrorMessage == "Время начала должно быть в будущем.");
    }

    [Fact]
    public void Validate_WithEndTimeNotAfterStart_Fails()
    {
        // Arrange
        var dto = ValidDto(start: FutureStart, end: FutureStart);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateScheduleSlotDto.EndTime)
                 && e.ErrorMessage == "Время окончания должно быть позже времени начала.");
    }

    [Theory]
    [InlineData(ScheduleSlot.MinDurationMinutes - 1)]
    [InlineData(ScheduleSlot.MaxDurationMinutes + 1)]
    public void Validate_WithDurationOutOfRange_Fails(int durationMinutes)
    {
        // Arrange
        var dto = ValidDto(
            start: FutureStart,
            end: FutureStart.AddMinutes(durationMinutes));

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.ErrorMessage == $"Длительность слота должна быть " +
                 $"от {ScheduleSlot.MinDurationMinutes} " +
                 $"до {ScheduleSlot.MaxDurationMinutes} минут.");
    }

    [Theory]
    [InlineData(ScheduleSlot.MinDurationMinutes)]
    [InlineData(ScheduleSlot.MaxDurationMinutes)]
    public void Validate_WithDurationAtBounds_Passes(int durationMinutes)
    {
        // Arrange
        var dto = ValidDto(
            start: FutureStart,
            end: FutureStart.AddMinutes(durationMinutes));

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }
}
