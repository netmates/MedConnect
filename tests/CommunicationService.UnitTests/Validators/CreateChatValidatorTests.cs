using CommunicationService.Features.Chats.Create;

namespace CommunicationService.UnitTests.Validators;

public class CreateChatValidatorTests
{
    private readonly CreateChatValidator _validator = new();

    [Fact]
    public void Validate_WithAppointmentId_Passes()
    {
        // Arrange
        var request = new CreateChatRequest(Guid.NewGuid());

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithEmptyAppointmentId_Fails()
    {
        // Arrange
        var request = new CreateChatRequest(Guid.Empty);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(CreateChatRequest.AppointmentId)
                     && error.ErrorMessage == "AppointmentId обязателен.");
    }
}
