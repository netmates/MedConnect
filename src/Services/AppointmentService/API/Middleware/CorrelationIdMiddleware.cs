using AppointmentService.Application.Common;
using Serilog.Context;

namespace AppointmentService.API.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);

        context.Items[CorrelationIdKeys.ItemKey] = correlationId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdKeys.HeaderName))
                context.Response.Headers[CorrelationIdKeys.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(CorrelationIdKeys.ItemKey, correlationId))
        {
            await next(context);
        }
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdKeys.HeaderName, out var values))
        {
            var incoming = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(incoming))
                return incoming.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();
}
