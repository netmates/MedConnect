namespace AppointmentService.Application.Interfaces.Services;

public interface IKeycloakAdminService
{
    /// <summary>
    /// Создаёт пользователя в Keycloak и возвращает его KeycloakId.
    /// </summary>
    Task<string> CreateUserAsync(
        string email,
        string temporaryPassword,
        string role,
        string firstName,
        string lastName,
        CancellationToken ct = default);
    /// <summary>
    /// Удаляет пользователя в Keycloak по KeycloakId.
    /// </summary>
    Task DeleteUserAsync(string keycloakId, CancellationToken ct = default);
    /// <summary>
    /// Блокирует пользователя (enabled = false).
    /// </summary>
    Task DisableUserAsync(string keycloakId, CancellationToken ct = default);
    /// <summary>
    /// Разблокирует пользователя (enabled = true).
    /// </summary>
    Task EnableUserAsync(string keycloakId, CancellationToken ct = default);
    /// <summary>
    /// Сбрасывает пароль пользователя.
    /// </summary>
    Task ResetPasswordAsync(string keycloakId, string newPassword, CancellationToken ct = default);
}
