using AppointmentService.Application.DTOs.Specialization;
using AppointmentService.Application.Validators;
using AppointmentService.Domain.Entities;
using FluentValidation.Results;

namespace AppointmentService.UnitTests.Validators;

public class CreateSpecializationValidatorTests
{
    private readonly CreateSpecializationValidator _validator = new();

    private static CreateSpecializationDto ValidDto(string? name = null)
        => new()
        {
            Name = name ?? "Терапия"
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
    public void Validate_WithEmptyOrWhitespaceName_Fails(string name)
    {
        // Arrange
        var dto = ValidDto(name: name);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(CreateSpecializationDto.Name)
                 && e.ErrorMessage == "Название специализации обязательно.");
    }

    [Fact]
    public void Validate_WithNameTooLong_Fails()
    {
        // Arrange
        var dto = ValidDto(name: new string('а', Specialization.MaxNameLength + 1));

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(CreateSpecializationDto.Name)
                 && e.ErrorMessage ==
                    $"Название специализации не должно превышать {Specialization.MaxNameLength} символов.");
    }

    [Fact]
    public void Validate_WithNameAtMaxLength_Passes()
    {
        // Arrange
        var dto = ValidDto(name: new string('а', Specialization.MaxNameLength));

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.True(result.IsValid);
    }
}
