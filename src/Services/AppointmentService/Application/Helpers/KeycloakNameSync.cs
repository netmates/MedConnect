using AppointmentService.Application.Interfaces.Services;

namespace AppointmentService.Application.Helpers;

public static class KeycloakNameSync
{
    public static bool HasNameChanged(
        string currentFirstName,
        string currentLastName,
        string newFirstName,
        string newLastName)
    {
        return !string.Equals(currentFirstName, newFirstName.Trim(), StringComparison.Ordinal)
            || !string.Equals(currentLastName, newLastName.Trim(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Если ФИО изменилось — обновляет имя в Keycloak. Возвращает true, если вызов был.
    /// </summary>
    public static async Task<bool> ApplyIfChangedAsync(
        IKeycloakAdminService keycloak,
        string keycloakId,
        string currentFirstName,
        string currentLastName,
        string newFirstName,
        string newLastName,
        CancellationToken ct)
    {
        if (!HasNameChanged(currentFirstName, currentLastName, newFirstName, newLastName))
            return false;

        await keycloak.UpdateUserNameAsync(keycloakId, newFirstName.Trim(), newLastName.Trim(), ct);

        return true;
    }

    /// <summary>
    /// Откатывает ФИО в Keycloak после неудачного DB-update. Ошибки compensate только логирует.
    /// </summary>
    public static async Task CompensateIfNeededAsync(
        IKeycloakAdminService keycloak,
        ILogger logger,
        bool nameChanged,
        Exception dbFailure,
        string keycloakId,
        string oldFirstName,
        string oldLastName,
        string entityLogTemplate,
        params object[] entityLogArgs)
    {
        if (!nameChanged)
            return;

        logger.LogError(
            dbFailure,
            "DB update failed after Keycloak name update. Compensating. " + entityLogTemplate,
            entityLogArgs);

        try
        {
            await keycloak.UpdateUserNameAsync(keycloakId, oldFirstName, oldLastName, CancellationToken.None);
        }
        catch (Exception compensateEx)
        {
            logger.LogError(
                compensateEx,
                "Failed to compensate Keycloak name for {KeycloakId}",
                keycloakId);
        }
    }
}
