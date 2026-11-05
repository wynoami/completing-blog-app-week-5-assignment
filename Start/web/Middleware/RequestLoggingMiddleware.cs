using System.Diagnostics;

namespace AtlasBlog.Middleware;

/// <summary>
/// Logs one line for every request: method, path, response status code,
/// how long it took, and who made it. Request bodies are never logged,
/// because they can contain passwords.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Times the rest of the pipeline, then logs the result.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            // "finally" runs even if something above threw an exception
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var user = context.User.Identity?.IsAuthenticated == true
                ? context.User.Identity.Name
                : "anonymous";

            // Errors stand out in red, client mistakes (like 404) in yellow
            var level = statusCode >= 500 ? LogLevel.Error
                      : statusCode >= 400 ? LogLevel.Warning
                      : LogLevel.Information;

            _logger.Log(level, "{Method} {Path} responded {StatusCode} in {ElapsedMs} ms (user: {User})",
                context.Request.Method, context.Request.Path, statusCode,
                stopwatch.ElapsedMilliseconds, user);
        }
    }
}