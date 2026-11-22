using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ApiGateway.Auth;

public sealed class TokenValidationMiddleware(RequestDelegate next)
{
    private static readonly PathString ApiPrefix = new("/api");
    private static readonly PathString SignUpPath = new("/api/patients/sign-up");

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresToken(context.Request))
        {
            await next(context);
            return;
        }

        var result = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        context.User = result.Principal!;
        await next(context);
    }

    private static bool RequiresToken(HttpRequest request)
    {
        if (!request.Path.StartsWithSegments(ApiPrefix))
            return false;

        if (HttpMethods.IsPost(request.Method) && request.Path.Equals(SignUpPath, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
