using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.Validators;
using FluentValidation.Results;

namespace AppointmentService.UnitTests.Validators;

public class ResetPasswordValidatorTests
{
    private const int MinPasswordLength = 8;

    private readonly ResetPasswordValidator _validator = new();

    private static ResetPasswordDto ValidDto(string? password = null)
        => new()
        {
            NewPassword = password ?? "TempPass1!"
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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespacePassword_Fails(string password)
    {
        // Arrange
        var dto = ValidDto(password);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(ResetPasswordDto.NewPassword)
                 && e.ErrorMessage == "Новый пароль обязателен.");
    }

    [Fact]
    public void Validate_WithPasswordTooShort_Fails()
    {
        // Arrange
        var dto = ValidDto(new string('a', MinPasswordLength - 1));

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(ResetPasswordDto.NewPassword)
                 && e.ErrorMessage ==
                    $"Новый пароль должен содержать минимум {MinPasswordLength} символов.");
    }

    [Fact]
    public void Validate_WithPasswordAtMinLength_Passes()
    {
        // Arrange
        var dto = ValidDto(new string('a', MinPasswordLength));

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }
}
