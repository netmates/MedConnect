using AppointmentService.Application.DTOs.Doctor;
using AppointmentService.Application.Validators;
using AppointmentService.Domain.Entities;
using FluentValidation.Results;

namespace AppointmentService.UnitTests.Validators;

public class CreateDoctorValidatorTests
{
    private readonly CreateDoctorValidator _validator = new();

    private static CreateDoctorDto ValidDto()
        => new()
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
        var dto = ValidDto();
        dto.LastName = "  ";

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
            e => e.PropertyName == nameof(CreateDoctorDto.LastName)
                 && e.ErrorMessage == $"Фамилия не должна превышать {Doctor.MaxLastNameLength} символов.");
    }

    [Fact]
    public void Validate_WithEmptyFirstName_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.FirstName = "";

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(CreateDoctorDto.FirstName)
                 && e.ErrorMessage == "Имя обязательно.");
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
            e => e.PropertyName == nameof(CreateDoctorDto.MiddleName)
                 && e.ErrorMessage == $"Отчество не должно превышать {Doctor.MaxMiddleNameLength} символов.");
    }

    [Fact]
    public void Validate_WithEmptyEmail_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.Email = "";

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
        var dto = ValidDto();
        dto.Email = "not-an-email";

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
        var dto = ValidDto();
        dto.TemporaryPassword = "";

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(CreateDoctorDto.TemporaryPassword)
                 && e.ErrorMessage == "Временный пароль обязателен.");
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
    public void Validate_WithExperienceOutOfRange_Fails(int experienceYears, string expectedMessage)
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
            e => e.PropertyName == nameof(CreateDoctorDto.ExperienceYears)
                 && e.ErrorMessage == expectedMessage);
    }

    [Fact]
    public void Validate_WithEmptyDescription_Fails()
    {
        // Arrange
        var dto = ValidDto();
        dto.Description = "  ";

        // Act
        ValidationResult result = _validator.Validate(dto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            e => e.PropertyName == nameof(CreateDoctorDto.Description)
                 && e.ErrorMessage == "Описание обязательно.");
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
            e => e.PropertyName == nameof(CreateDoctorDto.SpecializationIds)
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
            e => e.PropertyName == nameof(CreateDoctorDto.SpecializationIds)
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
            e => e.PropertyName == nameof(CreateDoctorDto.SpecializationIds)
                 && e.ErrorMessage == "Дублирующиеся специализации не допускаются.");
    }
}
