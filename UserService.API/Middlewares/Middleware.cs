
namespace UserService.API.Middlewares
{
    public class TokenProcessingMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenProcessingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {

            var authHeader = httpContext.Request.Headers["Authorization"].ToString();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                // Remove "Bearer " part from the token
                httpContext.Request.Headers["Authorization"] = authHeader.Substring("Bearer ".Length).Trim();
            }

            await _next(httpContext);  
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenProcessingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenProcessingMiddleware>();
        }
    }
}
