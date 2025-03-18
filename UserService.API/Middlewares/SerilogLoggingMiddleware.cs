using Serilog.Context;
using System.Diagnostics;

namespace UserService.API.Middlewares
{
    public class SerilogLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public SerilogLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var requestId = Activity.Current?.Id ?? context.TraceIdentifier;

            using (LogContext.PushProperty("RequestId", requestId))
            using (LogContext.PushProperty("ServiceName", context.Request.Path))
            {
                await _next(context);
            }
        }
    }

    public static class SerilogLoggingMiddlewareExtension
    {
        public static IApplicationBuilder UseSerilogLoggingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SerilogLoggingMiddleware>();
        }
    }
}
