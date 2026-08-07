using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Exceptions;

namespace AppointmentService.UnitTests.Domain;

public class DoctorTests
{
    private static Doctor CreateValid(
        string keycloakId = "kc-doctor-1",
        string lastName = "Иванов",
        string firstName = "Иван",
        string? middleName = "Иванович",
        string description = "Терапевт",
        int experienceYears = 10)
        => Doctor.Create(keycloakId, lastName, firstName, middleName, description, experienceYears);

    [Fact]
    public void Create_WithValidData_TrimsFieldsAndSetsActive()
    {
        // Arrange
        // Act
        var doctor = Doctor.Create(
            "  kc-1  ",
            "  Иванов  ",
            "  Иван  ",
            "   ",
            "  Описание  ",
            5);

        // Assert
        Assert.Equal("kc-1", doctor.KeycloakId);
        Assert.Equal("Иванов", doctor.LastName);
        Assert.Equal("Иван", doctor.FirstName);
        Assert.Null(doctor.MiddleName);
        Assert.Equal("Описание", doctor.Description);
        Assert.Equal(5, doctor.ExperienceYears);
        Assert.True(doctor.IsActive);
        Assert.NotEqual(Guid.Empty, doctor.Id);
    }

    [Fact]
    public void Create_WithEmptyLastName_Throws()
    {
        // Arrange
        // Act
        var ex = Assert.Throws<DomainException>(() => CreateValid(lastName: "  "));

        // Assert
        Assert.Equal("Фамилия обязательна.", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyFirstName_Throws()
    {
        // Arrange
        // Act
        var ex = Assert.Throws<DomainException>(() => CreateValid(firstName: "  "));

        // Assert
        Assert.Equal("Имя обязательно.", ex.Message);
    }

    [Fact]
    public void Create_WithExperienceOutOfRange_Throws()
    {
        // Arrange
        // Act
        var ex = Assert.Throws<DomainException>(() => CreateValid(experienceYears: -1));

        // Assert
        Assert.Equal(
            $"Опыт должен быть от {Doctor.MinExperienceYears} до {Doctor.MaxExperienceYears} лет.",
            ex.Message);
    }

    [Fact]
    public void Update_WithValidData_ChangesProfile()
    {
        // Arrange
        var doctor = CreateValid();

        // Act
        doctor.Update("Петров", "Петр", null, "Хирург", 20);

        // Assert
        Assert.Equal("Петров", doctor.LastName);
        Assert.Equal("Петр", doctor.FirstName);
        Assert.Null(doctor.MiddleName);
        Assert.Equal("Хирург", doctor.Description);
        Assert.Equal(20, doctor.ExperienceYears);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        // Arrange
        var doctor = CreateValid();

        // Act
        doctor.Deactivate();

        // Assert
        Assert.False(doctor.IsActive);
    }
}
