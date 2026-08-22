using AppointmentService.Infrastructure.Keycloak;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppointmentService.API.OpenApi;

/// <summary>
/// Добавляет в OpenAPI схему OAuth2 Password (Keycloak),
/// чтобы Scalar мог запросить token по логину/паролю.
/// Работает рядом с BearerSecuritySchemeTransformer (ручная вставка JWT).
/// </summary>
internal sealed class KeycloakSecuritySchemeTransformer(
    IConfiguration configuration) : IOpenApiDocumentTransformer
{
    public const string SchemeId = "OAuth2";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var tokenUrl = KeycloakConfiguration.GetTokenEndpoint(configuration);

        var oauth2 = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Description =
                "Keycloak Resource Owner Password. " +
                "Примеры: admin1 / doctor1@medconnect.local / patient1@medconnect.local. " +
                "ClientId: medconnect-app (без client secret).",
            Flows = new OpenApiOAuthFlows
            {
                Password = new OpenApiOAuthFlow
                {
                    TokenUrl = tokenUrl,
                    Scopes = new Dictionary<string, string>
                    {
                        ["openid"] = "OpenID Connect"
                    }
                }
            }
        };

        document.Components ??= new OpenApiComponents();
        document.AddComponent(SchemeId, oauth2);
        
        document.Security ??= [];
        document.Security.Add(
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeId, document)] = ["openid"]
            });

        return Task.CompletedTask;
    }
}
