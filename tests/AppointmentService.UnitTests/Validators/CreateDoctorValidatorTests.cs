using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.Validators;
using FluentValidation.Results;

namespace AppointmentService.UnitTests.Validators;

public class CreateDoctorValidatorTests
{
    private readonly CreateDoctorValidator _validator = new();

    private static CreateDoctorDto ValidDto() => new()
    {
        LastName = "Иванов",
        FirstName = "Иван",
        MiddleName = "Иванович",
        Email = "doctor@medconnect.local",
        TemporaryPassword = "TempPass1!",
        Description = "Терапевт",
        ExperienceYears = 10,
        SpecializationIds = [Guid.NewGuid()]
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
            e => e.PropertyName == nameof(CreateDoctorDto.LastName)
                 && e.ErrorMessage == "Фамилия обязательна.");
    }

    [Fact]
    public void Validate_WithEmptyEmail_Fails()
    {
        // Arrange
        var dto = ValidDto() with { Email = "" };

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(CreateDoctorDto.Email)
                 && e.ErrorMessage == "Email обязателен.");
    }

    [Fact]
    public void Validate_WithInvalidEmail_Fails()
    {
        // Arrange
        var dto = ValidDto() with { Email = "not-an-email" };

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(CreateDoctorDto.Email)
                 && e.ErrorMessage == "Некорректный email.");
    }

    [Fact]
    public void Validate_WithEmptyTemporaryPassword_Fails()
    {
        // Arrange
        var dto = ValidDto() with { TemporaryPassword = "" };

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(CreateDoctorDto.TemporaryPassword)
                 && e.ErrorMessage == "Временный пароль обязателен.");
    }
}
