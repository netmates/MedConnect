using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Exceptions;

namespace AppointmentService.UnitTests.Domain;

public class PatientTests
{
    private static readonly DateTime ValidDateOfBirth =
        new(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc);

    private static Patient CreateValid(
        string keycloakId = "kc-patient-1",
        string lastName = "Сидоров",
        string firstName = "Сидор",
        string? middleName = "Сидорович",
        string? phone = "+79001234567",
        DateTime? dateOfBirth = null)
        => Patient.Create(
            keycloakId,
            lastName,
            firstName,
            middleName,
            phone,
            dateOfBirth ?? ValidDateOfBirth);

    [Fact]
    public void Create_WithValidData_TrimsFieldsAndSetsActive()
    {
        // Arrange
        // Act
        var patient = Patient.Create(
            "  kc-p  ",
            "  Сидоров  ",
            "  Сидор  ",
            "   ",
            "  +79001112233  ",
            ValidDateOfBirth);

        // Assert
        Assert.Equal("kc-p", patient.KeycloakId);
        Assert.Equal("Сидоров", patient.LastName);
        Assert.Equal("Сидор", patient.FirstName);
        Assert.Null(patient.MiddleName);
        Assert.Equal("+79001112233", patient.Phone);
        Assert.Equal(ValidDateOfBirth, patient.DateOfBirth);
        Assert.True(patient.IsActive);
        Assert.NotEqual(Guid.Empty, patient.Id);
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
    public void Create_WithInvalidPhone_Throws()
    {
        // Arrange
        // Act
        var ex = Assert.Throws<DomainException>(() => CreateValid(phone: "abc"));

        // Assert
        Assert.Equal("Некорректный формат телефона.", ex.Message);
    }

    [Fact]
    public void Create_WithDateOfBirthInFuture_Throws()
    {
        // Arrange
        var future = DateTime.UtcNow.Date.AddDays(1);

        // Act
        var ex = Assert.Throws<DomainException>(() => CreateValid(dateOfBirth: future));

        // Assert
        Assert.Equal("Дата рождения должна быть в прошлом.", ex.Message);
    }

    [Fact]
    public void Update_WithValidData_ChangesProfile()
    {
        // Arrange
        var patient = CreateValid();
        var dob = new DateTime(1985, 3, 10, 0, 0, 0, DateTimeKind.Utc);

        // Act
        patient.Update("Козлов", "Игорь", null, "+79998887766", dob);

        // Assert
        Assert.Equal("Козлов", patient.LastName);
        Assert.Equal("Игорь", patient.FirstName);
        Assert.Null(patient.MiddleName);
        Assert.Equal("+79998887766", patient.Phone);
        Assert.Equal(dob, patient.DateOfBirth);
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        // Arrange
        var patient = CreateValid();

        // Act
        patient.Deactivate();

        // Assert
        Assert.False(patient.IsActive);
    }
}
