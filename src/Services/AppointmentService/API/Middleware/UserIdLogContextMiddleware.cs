using System.Security.Claims;
using Serilog.Context;

namespace AppointmentService.API.Middleware;

public sealed class UserIdLogContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirstValue("sub")
            ?? context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            using (LogContext.PushProperty("UserId", userId))
            {
                await next(context);
                return;
            }
        }

        await next(context);
    }
}

public static class UserIdLogContextMiddlewareExtensions
{
    public static IApplicationBuilder UseUserIdLogContext(this IApplicationBuilder app)
        => app.UseMiddleware<UserIdLogContextMiddleware>();
}
