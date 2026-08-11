namespace AppointmentService.Infrastructure.Services;

public interface IKeycloakTokenCache
{
    string? Token { get; }
    DateTime ExpiresAt { get; }
    void Set(string token, DateTime expiresAt);
}

public sealed class KeycloakTokenCache : IKeycloakTokenCache
{
    private readonly Lock _lock = new();
    private string? _token;
    private DateTime _expiresAt = DateTime.MinValue;

    public string? Token { get { lock (_lock) return _token; } }
    public DateTime ExpiresAt { get { lock (_lock) return _expiresAt; } }

    public void Set(string token, DateTime expiresAt)
    {
        lock (_lock)
        {
            _token = token;
            _expiresAt = expiresAt;
        }
    }
}
