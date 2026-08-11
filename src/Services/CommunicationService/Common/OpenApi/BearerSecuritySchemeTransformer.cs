using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace CommunicationService.Common.OpenApi;

/// <summary>
/// Добавляет в OpenAPI схему HTTP Bearer (JWT),
/// чтобы Scalar мог принимать вручную вставленный access_token из Keycloak.
/// </summary>
internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public const string SchemeId = "Bearer";

    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!schemes.Any(s => s.Name == SchemeId)) return;

        var bearer = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT из Keycloak. Вставьте access_token без префикса Bearer."
        };

        document.Components ??= new OpenApiComponents();
        document.AddComponent(SchemeId, bearer);

        document.Security ??= [];
        document.Security.Add(
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeId, document)] = []
            });
    }
}
