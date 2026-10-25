using MedConnect.Shared.Auth;
using Scalar.AspNetCore;

namespace MedConnect.Shared.OpenApi;

public static class ScalarExtensions
{
    public static void MapKeycloakScalar(this WebApplication app, string username)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            var clientId = KeycloakConfiguration.GetRequired(app.Configuration, nameof(KeycloakOptions.Audience));

            options
                .AddPreferredSecuritySchemes(KeycloakSecuritySchemeTransformer.SchemeId)
                .AddPasswordFlow(KeycloakSecuritySchemeTransformer.SchemeId, flow =>
                {
                    flow.ClientId = clientId;
                    flow.Username = username;
                    flow.SelectedScopes = [KeycloakSecuritySchemeTransformer.OpenIdScope];
                    flow.WithCredentialsLocation(CredentialsLocation.Body);
                });
        });
    }
}
