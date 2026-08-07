using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.Validators;
using AppointmentService.Domain.Entities;
using FluentValidation.Results;

namespace AppointmentService.UnitTests.Validators;

public class UpdateDoctorValidatorTests
{
    private readonly UpdateDoctorValidator _validator = new();

    private static UpdateDoctorDto ValidDto()
        => new()
        {
            LastName = "Иванов",
            FirstName = "Иван",
            MiddleName = "Иванович",
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
    public void Validate_WithNullMiddleName_Passes()
    {
        // Arrange
        var dto = ValidDto();
        dto.MiddleName = null;

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
            e => e.PropertyName == nameof(UpdateDoctorDto.LastName)
                 && e.ErrorMessage == "Фамилия обязательна.");
    }

    [Fact]
    public void Validate_WithLastNameTooLong_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.LastName = new string('а', Doctor.MaxLastNameLength + 1);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateDoctorDto.LastName)
                 && e.ErrorMessage ==
                    $"Фамилия не должна превышать {Doctor.MaxLastNameLength} символов.");
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
            e => e.PropertyName == nameof(UpdateDoctorDto.FirstName)
                 && e.ErrorMessage == "Имя обязательно.");
    }

    [Fact]
    public void Validate_WithFirstNameTooLong_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.FirstName = new string('а', Doctor.MaxFirstNameLength + 1);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateDoctorDto.FirstName)
                 && e.ErrorMessage ==
                    $"Имя не должно превышать {Doctor.MaxFirstNameLength} символов.");
    }

    [Fact]
    public void Validate_WithMiddleNameTooLong_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.MiddleName = new string('а', Doctor.MaxMiddleNameLength + 1);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateDoctorDto.MiddleName)
                 && e.ErrorMessage ==
                    $"Отчество не должно превышать {Doctor.MaxMiddleNameLength} символов.");
    }

    public static TheoryData<int, string> ExperienceOutOfRangeCases => new()
    {
        {
            Doctor.MinExperienceYears - 1,
            $"Опыт не может быть меньше {Doctor.MinExperienceYears} лет."
        },
        {
            Doctor.MaxExperienceYears + 1,
            $"Опыт не может быть больше {Doctor.MaxExperienceYears} лет."
        }
    };

    [Theory]
    [MemberData(nameof(ExperienceOutOfRangeCases))]
    public void Validate_WithExperienceOutOfRange_Fails(
        int experienceYears,
        string expectedMessage)
    {
        // Arrange
        var dto = ValidDto();
        dto.ExperienceYears = experienceYears;

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateDoctorDto.ExperienceYears)
                 && e.ErrorMessage == expectedMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyOrWhitespaceDescription_Fails(string description)
    {
        // Arrange
        var dto = ValidDto();
        dto.Description = description;

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateDoctorDto.Description)
                 && e.ErrorMessage == "Описание обязательно.");
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.Description = new string('а', Doctor.MaxDescriptionLength + 1);

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateDoctorDto.Description)
                 && e.ErrorMessage ==
                    $"Описание не должно превышать {Doctor.MaxDescriptionLength} символов.");
    }

    [Fact]
    public void Validate_WithEmptySpecializationIds_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.SpecializationIds = [];

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateDoctorDto.SpecializationIds)
                 && e.ErrorMessage == "Укажите хотя бы одну специализацию.");
    }

    [Fact]
    public void Validate_WithEmptyGuidInSpecializationIds_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.SpecializationIds = [Guid.Empty];

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateDoctorDto.SpecializationIds)
                 && e.ErrorMessage == "Идентификатор специализации некорректный.");
    }

    [Fact]
    public void Validate_WithDuplicateSpecializationIds_Fails()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = ValidDto();
        dto.SpecializationIds = [id, id];

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(UpdateDoctorDto.SpecializationIds)
                 && e.ErrorMessage == "Дублирующиеся специализации не допускаются.");
    }
}
