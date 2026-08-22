namespace CommunicationService.Common.Auth;

public sealed class KeycloakOptions
{
    public const string SectionName = "Keycloak";
    public const string AdminClientIdKey = "AdminClientId";
    public const string AdminClientSecretKey = "AdminClientSecret";

    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string AdminApiUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
}

public static class KeycloakConfiguration
{
    public static string GetRequired(IConfiguration configuration, string key)
    {
        var path = $"{KeycloakOptions.SectionName}:{key}";
        return configuration[path]
            ?? throw new InvalidOperationException($"{path} не задан.");
    }

    public static Uri GetRealmUri(IConfiguration configuration)
    {
        var adminApiUrl = GetRequired(configuration, nameof(KeycloakOptions.AdminApiUrl));
        var realm = GetRequired(configuration, nameof(KeycloakOptions.Realm));
        return new Uri($"{adminApiUrl.TrimEnd('/')}/realms/{realm}");
    }

    public static Uri GetTokenEndpoint(IConfiguration configuration) =>
        new($"{GetRealmUri(configuration).AbsoluteUri.TrimEnd('/')}/protocol/openid-connect/token");
}
