using Serilog.Context;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
            // Lấy thông tin UserId từ Claims trong JWT Token
            var userId = context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? "Anonymous";
            var userRole = context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "NoRole";
            var requestPath = context.Request?.Path.Value ?? "Unknown";
            var traceId = context.TraceIdentifier;

            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("UserRole", userRole))
            using (LogContext.PushProperty("ServiceName", "UserService"))
            using (LogContext.PushProperty("RequestPath", requestPath))
            using (LogContext.PushProperty("TraceId", traceId))
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
