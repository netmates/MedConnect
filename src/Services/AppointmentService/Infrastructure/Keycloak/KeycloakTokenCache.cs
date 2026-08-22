namespace AppointmentService.Infrastructure.Keycloak;

public interface IKeycloakTokenCache
{
    bool TryGetValid(out string token);
    void Set(string token, DateTime expiresAt);
}

public sealed class KeycloakTokenCache : IKeycloakTokenCache
{
    private readonly Lock _lock = new();
    private string? _token;
    private DateTime _expiresAt = DateTime.MinValue;

    public bool TryGetValid(out string token)
    {
        lock (_lock)
        {
            if (_token is not null && DateTime.UtcNow < _expiresAt)
            {
                token = _token;
                return true;
            }

            token = null!;
            return false;
        }
    }

    public void Set(string token, DateTime expiresAt)
    {
        lock (_lock)
        {
            _token = token;
            _expiresAt = expiresAt;
        }
    }
}
