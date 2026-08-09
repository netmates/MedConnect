using AppointmentService.Application.Interfaces.Services;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AppointmentService.Infrastructure.Services;

public class KeycloakAdminService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<KeycloakAdminService> logger,
    IKeycloakTokenCache tokenCache) : IKeycloakAdminService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<KeycloakAdminService> _logger = logger;
    private readonly IKeycloakTokenCache _tokenCache = tokenCache;
    private const int TokenExpiryBufferSeconds = 30;

    public async Task<string> CreateUserAsync(
        string email,
        string temporaryPassword,
        string role,
        string firstName,
        string lastName,
        CancellationToken ct = default)
    {
        var adminToken = await GetAdminTokenAsync(ct);
        var realm = _configuration["Keycloak:Realm"]!;

        var payload = new
        {
            username = email,
            email,
            enabled = true,
            firstName,
            lastName,
            credentials = new[]
            {
                new { type = "password", value = temporaryPassword, temporary = true }
            }            
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/admin/realms/{realm}/users")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var location = response.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("Keycloak не вернул Location header.");

        var keycloakId = location.Split('/').Last();

        try
        {
            await AssignRealmRoleAsync(keycloakId, role, ct);
            if (role == "doctor")
                await RemoveRealmRoleAsync(keycloakId, "patient", ct);
        }
        catch
        {
            try
            {
                await DeleteUserAsync(keycloakId, CancellationToken.None);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(
                    rollbackEx,
                    "Failed to rollback Keycloak user {KeycloakId} after role assignment failure",
                    keycloakId);
            }

            throw;
        }

        _logger.LogInformation(
            "Keycloak user created: {KeycloakId} for {Email} with role {Role}",
            keycloakId, email, role);

        return keycloakId;
    }

    public async Task DeleteUserAsync(string keycloakId, CancellationToken ct = default)
    {
        var adminToken = await GetAdminTokenAsync(ct);
        var realm = _configuration["Keycloak:Realm"]!;

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/admin/realms/{realm}/users/{keycloakId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Keycloak user deleted: {KeycloakId}", keycloakId);
    }

    public async Task DisableUserAsync(string keycloakId, CancellationToken ct = default)
    {
        var adminToken = await GetAdminTokenAsync(ct);
        var realm = _configuration["Keycloak:Realm"]!;

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/admin/realms/{realm}/users/{keycloakId}")
        {
            Content = JsonContent.Create(new { enabled = false })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task EnableUserAsync(string keycloakId, CancellationToken ct = default)
    {
        var adminToken = await GetAdminTokenAsync(ct);
        var realm = _configuration["Keycloak:Realm"]!;

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/admin/realms/{realm}/users/{keycloakId}")
        {
            Content = JsonContent.Create(new { enabled = true })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResetPasswordAsync(string keycloakId, string newPassword, CancellationToken ct = default)
    {
        var adminToken = await GetAdminTokenAsync(ct);
        var realm = _configuration["Keycloak:Realm"]!;
        var payload = new { type = "password", value = newPassword, temporary = false };

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"/admin/realms/{realm}/users/{keycloakId}/reset-password")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Получает access-токен Keycloak Admin API (client_credentials), с кешированием до истечения.
    /// </summary>
    private async Task<string> GetAdminTokenAsync(CancellationToken ct)
    {
        if (_tokenCache.Token is not null && DateTime.UtcNow < _tokenCache.ExpiresAt)
            return _tokenCache.Token;

        var tokenRequest = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _configuration["Keycloak:AdminClientId"]!),
            new KeyValuePair<string, string>("client_secret", _configuration["Keycloak:AdminClientSecret"]!)
        ]);

        var realm = _configuration["Keycloak:Realm"]!;

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/realms/{realm}/protocol/openid-connect/token")
        {
            Content = tokenRequest
        };

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var token = json.GetProperty("access_token").GetString()!;
        var expiresIn = json.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 60;
        var expiresAt = DateTime.UtcNow.AddSeconds(Math.Max(expiresIn - TokenExpiryBufferSeconds, 1));

        _tokenCache.Set(token, expiresAt);
        return token;
    }

    /// <summary>
    /// Назначает realm-роль пользователю.
    /// </summary>
    private async Task AssignRealmRoleAsync(string keycloakId, string roleName, CancellationToken ct = default)
    {
        var adminToken = await GetAdminTokenAsync(ct);
        var realm = _configuration["Keycloak:Realm"]!;
        var role = await GetRealmRoleAsync(adminToken, realm, roleName, ct);

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/admin/realms/{realm}/users/{keycloakId}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { role })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Снимает realm-роль с пользователя.
    /// </summary>
    private async Task RemoveRealmRoleAsync(string keycloakId, string roleName, CancellationToken ct = default)
    {
        var adminToken = await GetAdminTokenAsync(ct);
        var realm = _configuration["Keycloak:Realm"]!;
        var role = await GetRealmRoleAsync(adminToken, realm, roleName, ct);

        using var request = new HttpRequestMessage(
            HttpMethod.Delete,
            $"/admin/realms/{realm}/users/{keycloakId}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { role })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return;
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Получает представление realm-роли из Keycloak (нужно для role-mappings API).
    /// </summary>
    private async Task<JsonElement> GetRealmRoleAsync(
        string adminToken, string realm, string roleName, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/admin/realms/{realm}/roles/{roleName}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
    }
}
