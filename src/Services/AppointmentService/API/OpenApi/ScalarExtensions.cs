using Scalar.AspNetCore;

namespace AppointmentService.API.OpenApi;

public static class ScalarExtensions
{
    public static void MapCommunicationScalar(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            var clientId = app.Configuration["Keycloak:Audience"]
                ?? throw new InvalidOperationException("Keycloak:Audience не задан.");

            options
                .AddPreferredSecuritySchemes(KeycloakSecuritySchemeTransformer.SchemeId)
                .AddPasswordFlow(KeycloakSecuritySchemeTransformer.SchemeId, flow =>
                {
                    flow.ClientId = clientId;
                    flow.Username = "admin1";
                    flow.SelectedScopes = ["openid"];
                    flow.WithCredentialsLocation(CredentialsLocation.Body);
                });
        });
    }
}
