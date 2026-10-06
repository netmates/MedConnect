namespace AppointmentService.Application.Interfaces.Services;

public interface IKeycloakAdminService
{
    /// <summary>
    /// Создает пользователя в Keycloak и возвращает его KeycloakId.
    /// </summary>
    public Task<string> CreateUserAsync(
        string email,
        string temporaryPassword,
        string role,
        string firstName,
        string lastName,
        CancellationToken ct = default);
    /// <summary>
    /// Удаляет пользователя в Keycloak по KeycloakId.
    /// </summary>
    public Task DeleteUserAsync(string keycloakId, CancellationToken ct = default);
    /// <summary>
    /// Блокирует пользователя (enabled = false).
    /// </summary>
    public Task DisableUserAsync(string keycloakId, CancellationToken ct = default);
    /// <summary>
    /// Разблокирует пользователя (enabled = true).
    /// </summary>
    public Task EnableUserAsync(string keycloakId, CancellationToken ct = default);
    /// <summary>
    /// Сбрасывает пароль пользователя.
    /// </summary>
    public Task ResetPasswordAsync(string keycloakId, string newPassword, CancellationToken ct = default);
    /// <summary>
    /// Обновляет имя и фамилию пользователя в Keycloak.
    /// </summary>
    public Task UpdateUserNameAsync(string keycloakId, string firstName, string lastName, CancellationToken ct = default);
}
