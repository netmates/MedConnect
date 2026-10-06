using System.Security.Claims;
using CommunicationService.Common.Auth;
using CommunicationService.Common.Exceptions;

namespace CommunicationService.UnitTests.Auth;

public class CurrentUserTests
{
    [Fact]
    public void GetKeycloakId_WithSub_ReturnsSub()
    {
        // Arrange
        var user = Principal("patient-1");

        // Act
        var keycloakId = CurrentUser.GetKeycloakId(user);

        // Assert
        Assert.Equal("patient-1", keycloakId);
    }

    [Fact]
    public void GetKeycloakId_WithoutSub_ThrowsForbidden()
    {
        // Arrange
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var ex = Assert.Throws<ForbiddenException>(() => CurrentUser.GetKeycloakId(user));

        // Assert
        Assert.Equal("В токене нет claim sub.", ex.Message);
    }

    [Fact]
    public void GetKeycloakId_WithBlankSub_ThrowsForbidden()
    {
        // Arrange
        var user = Principal("   ");

        // Act
        var ex = Assert.Throws<ForbiddenException>(() => CurrentUser.GetKeycloakId(user));

        // Assert
        Assert.Equal("В токене нет claim sub.", ex.Message);
    }

    private static ClaimsPrincipal Principal(string sub) =>
        new(new ClaimsIdentity([new Claim("sub", sub)]));
}
