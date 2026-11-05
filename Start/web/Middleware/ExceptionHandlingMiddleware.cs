using System.Diagnostics;
using System.Net;

namespace AtlasBlog.Middleware;

/// <summary>
/// Catches any undhandled exception thrown later in the pipeline, logs it,
/// and returns a clean error response instead of a crash or a stack trace.
/// API requests (/api/...) get a JSON error; page requests get a friendly HTML page.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Runs the rest of the pipeline and handles any exception it throws.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // A reference number the user can report and we can find in the logs
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            _logger.LogError(ex, "Unhandled exception for {Method} {Path} (trace ID {TraceId})",
                context.Request.Method, context.Request.Path, traceId);

            if (context.Response.HasStarted)
            {
                // Part of the page was already sent, so it's too late to replace it
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            if (context.Request.Path.StartsWithSegments("/api"))
            {
                await context.Response.WriteAsJsonAsync(new
                {
                    status = StatusCodes.Status500InternalServerError,
                    error = "An unexpected error ocurred. Please try again later.",
                    traceId
                });
            }
            else
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(BuildErrorPage(traceId));
            }
        }
    }

    /// <summary>A simple, styled error page that reveals no technical details.</summary>
    private static string BuildErrorPage(string traceId)
    {
        var safeTraceId = WebUtility.HtmlEncode(traceId);

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>Something went wrong - Atlas Blog</title>
                <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet">
            </head>
            <body<
                <div class="container text-center mt-5">
                    <h1 class="display-5">Something went wrong</h1>
                    <p class="lead">Sorry, we hit an unexpected error. Please try again in a moment.</p>
                    <p class="text-muted small">Reference: {{safeTraceId}}</p>
                    <a href="/" class="btn btn-primary">Back to Home</a>
                </div>
            </body>
            </html>
            """;
    }
}