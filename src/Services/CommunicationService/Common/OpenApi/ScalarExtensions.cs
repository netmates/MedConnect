using CommunicationService.Common.Auth;
using Scalar.AspNetCore;

namespace CommunicationService.Common.OpenApi;

public static class ScalarExtensions
{
    public static void MapCommunicationScalar(this WebApplication app)
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
                    flow.Username = "patient1@medconnect.local";
                    flow.SelectedScopes = ["openid"];
                    flow.WithCredentialsLocation(CredentialsLocation.Body);
                });
        });
    }
}
