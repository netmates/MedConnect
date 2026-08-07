using AppointmentService.Application.DTOs.Patient;
using AppointmentService.Application.Validators;
using AppointmentService.Domain.Entities;
using FluentValidation.Results;

namespace AppointmentService.UnitTests.Validators;

public class RegisterPatientValidatorTests
{
    private readonly RegisterPatientValidator _validator = new();

    private static RegisterPatientDto ValidDto()
        => new()
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
    public void Validate_WithOnlyRequiredFields_Passes()
    {
        // Arrange
        var dto = new RegisterPatientDto
        {
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = null,
            Phone = null,
            DateOfBirth = null
        };

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespaceLastName_Fails(string lastName)
    {
        // Arrange
        var dto = ValidDto();
        dto.LastName = lastName;

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(RegisterPatientDto.LastName)
                 && e.ErrorMessage == "Фамилия обязательна.");
    }

    [Fact]
    public void Validate_WithLastNameTooLong_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.LastName = new string('а', Patient.MaxLastNameLength + 1);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(RegisterPatientDto.LastName)
                 && e.ErrorMessage ==
                    $"Фамилия не должна превышать {Patient.MaxLastNameLength} символов.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespaceFirstName_Fails(string firstName)
    {
        // Arrange
        var dto = ValidDto();
        dto.FirstName = firstName;

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(RegisterPatientDto.FirstName)
                 && e.ErrorMessage == "Имя обязательно.");
    }

    [Fact]
    public void Validate_WithFirstNameTooLong_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.FirstName = new string('а', Patient.MaxFirstNameLength + 1);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(RegisterPatientDto.FirstName)
                 && e.ErrorMessage ==
                    $"Имя не должно превышать {Patient.MaxFirstNameLength} символов.");
    }

    [Fact]
    public void Validate_WithMiddleNameTooLong_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.MiddleName = new string('а', Patient.MaxMiddleNameLength + 1);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(RegisterPatientDto.MiddleName)
                 && e.ErrorMessage ==
                    $"Отчество не должно превышать {Patient.MaxMiddleNameLength} символов.");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abc")]
    [InlineData("+7900abc4567")]
    public void Validate_WithInvalidPhone_Fails(string phone)
    {
        // Arrange
        var dto = ValidDto();
        dto.Phone = phone;

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(RegisterPatientDto.Phone)
                 && e.ErrorMessage == "Некорректный формат номера телефона.");
    }

    [Theory]
    [InlineData("+79001234567")]
    [InlineData("89001234567")]
    [InlineData("123-4567")]
    [InlineData("+7 900 123-45-67")]
    public void Validate_WithValidPhoneFormats_Passes(string phone)
    {
        // Arrange
        var dto = ValidDto();
        dto.Phone = phone;

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithFutureDateOfBirth_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.DateOfBirth = DateTime.UtcNow.AddDays(1);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(RegisterPatientDto.DateOfBirth)
                 && e.ErrorMessage == "Дата рождения не может быть в будущем.");
    }

    [Fact]
    public void Validate_WithDateOfBirthTooOld_Fails()
    {   
        var dto = ValidDto();
        dto.DateOfBirth = Patient.MinDateOfBirth;

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(RegisterPatientDto.DateOfBirth)
                 && e.ErrorMessage == "Некорректная дата рождения.");
    }

    [Fact]
    public void Validate_WithValidDateOfBirth_Passes()
    {
        // Arrange
        var dto = ValidDto();
        dto.DateOfBirth = Patient.MinDateOfBirth.AddDays(1);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }
}
