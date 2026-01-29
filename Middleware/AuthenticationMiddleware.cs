namespace UserManagementAPI.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for public endpoints (Swagger UI, root, favicon, health checks)
            if (context.Request.Path == "/" ||
                context.Request.Path == "/favicon.ico" ||
                context.Request.Path.StartsWithSegments("/swagger") ||
                context.Request.Path.StartsWithSegments("/api-docs") ||
                context.Request.Path.StartsWithSegments("/health"))
            {
                await _next(context);
                return;
            }

            string ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            string method = context.Request.Method;
            string path = context.Request.Path;

            // Helper to return a 401 with a single structured log entry
            static async Task Unauthorized(HttpContext ctx, ILogger logger, string method, string path, string ip, string reason)
            {
                logger.LogWarning("Unauthorized {Method} {Path} from {IP}: {Reason}", method, path, ip, reason);
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsJsonAsync(new { error = reason });
            }

            // Get the authorization header
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(authHeader))
            {
                await Unauthorized(context, _logger, method, path, ip, "Missing or invalid authorization token.");
                return;
            }

            // Check if the token starts with "Bearer "
            if (!authHeader.StartsWith("Bearer "))
            {
                await Unauthorized(context, _logger, method, path, ip, "Invalid token format. Use 'Bearer <token>'.");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Validate token (simple validation: token should not be empty and must be at least 10 characters)
            if (string.IsNullOrWhiteSpace(token) || token.Length < 10)
            {
                await Unauthorized(context, _logger, method, path, ip, "Invalid token.");
                return;
            }

            _logger.LogInformation("Authenticated {Method} {Path} from {IP}", method, path, ip);
            await _next(context);
        }
    }
}
