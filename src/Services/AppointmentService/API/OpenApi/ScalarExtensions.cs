using AppointmentService.Infrastructure.Keycloak;
using Scalar.AspNetCore;

namespace AppointmentService.API.OpenApi;

public static class ScalarExtensions
{
    public static void MapAppointmentScalar(this WebApplication app)
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
                    flow.Username = "admin1";
                    flow.SelectedScopes = ["openid"];
                    flow.WithCredentialsLocation(CredentialsLocation.Body);
                });
        });
    }
}
