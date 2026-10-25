using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Validators;
using FluentValidation.Results;

namespace AppointmentService.UnitTests.Validators;

public class UpdatePatientValidatorTests
{
    private readonly UpdatePatientValidator _validator = new();

    private static UpdatePatientDto ValidDto() => new()
    {
        LastName = "Иванов",
        FirstName = "Иван",
        MiddleName = "Иванович",
        Phone = "+79001234567",
        DateOfBirth = new DateTime(1990, 5, 15)
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
    public void Validate_WithEmptyLastName_Fails()
    {
        // Arrange
        var dto = ValidDto() with { LastName = "" };

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdatePatientDto.LastName)
                 && e.ErrorMessage == "Фамилия обязательна.");
    }
}
