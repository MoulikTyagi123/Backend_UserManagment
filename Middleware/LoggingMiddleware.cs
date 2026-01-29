using System.Diagnostics;

namespace UserManagementAPI.Middleware
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoggingMiddleware> _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            // Log incoming request
            _logger.LogInformation(
                $"Incoming Request: {context.Request.Method} {context.Request.Path} " +
                $"from {context.Connection.RemoteIpAddress}");

            // Proceed without buffering the response body to avoid interfering with response stream
            await _next(context);

            stopwatch.Stop();

            // Log outgoing response
            _logger.LogInformation(
                $"Outgoing Response: {context.Request.Method} {context.Request.Path} " +
                $"=> {context.Response.StatusCode} " +
                $"(Duration: {stopwatch.ElapsedMilliseconds}ms)");
        }
    }
}
