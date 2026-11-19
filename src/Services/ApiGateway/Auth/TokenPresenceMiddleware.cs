namespace ApiGateway.Auth;

public sealed class TokenPresenceMiddleware(RequestDelegate next)
{
    private static readonly PathString ApiPrefix = new("/api");
    private static readonly PathString SignUpPath = new("/api/patients/sign-up");
    private static readonly PathString ChatHubPrefix = new("/api/chats/hub");

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresToken(context.Request) || HasToken(context.Request))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    }

    private static bool RequiresToken(HttpRequest request)
    {
        if (!request.Path.StartsWithSegments(ApiPrefix))
            return false;

        if (HttpMethods.IsPost(request.Method) && request.Path.Equals(SignUpPath, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static bool HasToken(HttpRequest request)
    {
        if (request.Path.StartsWithSegments(ChatHubPrefix)
            && request.Query.TryGetValue("access_token", out var accessToken)
            && !string.IsNullOrWhiteSpace(accessToken))
        {
            return true;
        }

        var authorization = request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;

        return !string.IsNullOrWhiteSpace(authorization["Bearer ".Length..]);
    }
}
