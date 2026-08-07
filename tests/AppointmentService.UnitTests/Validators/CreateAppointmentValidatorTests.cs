using AppointmentService.Application.DTOs.Appointment;
using AppointmentService.Application.Validators;
using AppointmentService.Domain.Entities;
using FluentValidation.Results;

namespace AppointmentService.UnitTests.Validators;

public class CreateAppointmentValidatorTests
{
    private readonly CreateAppointmentValidator _validator = new();

    private static CreateAppointmentDto ValidDto(string? reason = "Осмотр")
        => new()
        {
            SlotId = Guid.NewGuid(),
            Reason = reason
        };

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
    public void Validate_WithEmptySlotId_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.SlotId = Guid.Empty;

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(CreateAppointmentDto.SlotId)
                 && e.ErrorMessage == "SlotId обязателен.");
    }

    [Fact]
    public void Validate_WithNullReason_Passes()
    {
        // Arrange
        var dto = ValidDto(reason: null);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithReasonAtMaxLength_Passes()
    {
        // Arrange
        var dto = ValidDto(reason: new string('x', Appointment.MaxReasonLength));

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithReasonTooLong_Fails()
    {
        // Arrange
        var dto = ValidDto(reason: new string('x', Appointment.MaxReasonLength + 1));

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(CreateAppointmentDto.Reason)
                 && e.ErrorMessage == $"Причина не должна превышать {Appointment.MaxReasonLength} символов.");
    }
}
