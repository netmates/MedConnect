using CommunicationService.Features.Messages;

namespace CommunicationService.UnitTests.Validators;

public class SendMessageValidatorTests
{
    private readonly SendMessageValidator _validator = new();

    [Fact]
    public void Validate_WithText_Passes()
    {
        // Arrange
        var request = new SendMessageRequest("Здравствуйте");

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithBlankText_Fails(string text)
    {
        // Arrange
        var request = new SendMessageRequest(text);

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(SendMessageRequest.Text)
                     && error.ErrorMessage == "Текст сообщения обязателен.");
    }

    [Fact]
    public void Validate_WithTextLongerThan2000_Fails()
    {
        // Arrange
        var request = new SendMessageRequest(new string('а', 2001));

        // Act
        var result = _validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == nameof(SendMessageRequest.Text)
                     && error.ErrorMessage == "Текст сообщения не должен превышать 2000 символов.");
    }
}
