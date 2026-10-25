using AppointmentService.Application.DTOs.ScheduleSlot;
using AppointmentService.Application.Validators;
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
}
