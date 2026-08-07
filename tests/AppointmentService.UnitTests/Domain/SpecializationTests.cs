using AppointmentService.Domain.Entities;
using AppointmentService.Domain.Exceptions;

namespace AppointmentService.UnitTests.Domain;

public class SpecializationTests
{
    [Fact]
    public void Create_WithValidName_TrimsName()
    {
        // Arrange
        // Act
        var spec = Specialization.Create("  Терапевт  ");

        // Assert
        Assert.NotEqual(Guid.Empty, spec.Id);
        Assert.Equal("Терапевт", spec.Name);
    }

    [Fact]
    public void Create_WithEmptyName_Throws()
    {
        // Arrange
        // Act
        var ex = Assert.Throws<DomainException>(() => Specialization.Create("  "));

        // Assert
        Assert.Equal("Название специализации обязательно.", ex.Message);
    }

    [Fact]
    public void Create_WithNameTooLong_Throws()
    {
        // Arrange
        var name = new string('a', Specialization.MaxNameLength + 1);

        // Act
        var ex = Assert.Throws<DomainException>(() => Specialization.Create(name));

        // Assert
        Assert.Equal(
            $"Название не должно превышать {Specialization.MaxNameLength} символов.",
            ex.Message);
    }

    [Fact]
    public void Update_WithValidName_ChangesName()
    {
        // Arrange
        var spec = Specialization.Create("Терапевт");

        // Act
        spec.Update("  Кардиолог  ");

        // Assert
        Assert.Equal("Кардиолог", spec.Name);
    }
}
