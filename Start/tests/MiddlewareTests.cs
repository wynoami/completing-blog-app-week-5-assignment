using AtlasBlog.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AtlasBlog.Tests;

/// <summary>Tests for the global error handler.</summary>
public class ExceptionHandlingMiddlewareTests
{
    private readonly TestLogger<ExceptionHandlingMiddleware> _logger = new();

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();   // lets the test read what was written
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    // A "next" step in the pipeline that always crashes
    private static readonly RequestDelegate Crash =
        _ => throw new InvalidOperationException("Secret connection string: Data Source=blog.db");

    [Fact]
    public async Task WhenNothingGoesWrong_PassesRequestThroughUnchanged()
    {
        var context = CreateContext("/");
        var middleware = new ExceptionHandlingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return ctx.Response.WriteAsync("Hello");
        }, _logger);

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("Hello", await ReadBodyAsync(context));
        Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task PageRequest_WhenSomethingCrashes_Returns500WithFriendlyPage()
    {
        var context = CreateContext("/Community/ViewPost/1");
        var middleware = new ExceptionHandlingMiddleware(Crash, _logger);

        await middleware.InvokeAsync(context);

        var body = await ReadBodyAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Contains("text/html", context.Response.ContentType);
        Assert.Contains("Something went wrong", body);
    }

    [Fact]
    public async Task ApiRequest_WhenSomethingCrashes_Returns500WithJsonError()
    {
        var context = CreateContext("/api/auth/registeradmin");
        var middleware = new ExceptionHandlingMiddleware(Crash, _logger);

        await middleware.InvokeAsync(context);

        var body = await ReadBodyAsync(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Contains("application/json", context.Response.ContentType);
        Assert.Contains("\"status\":500", body);
        Assert.Contains("traceId", body);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/api/auth/registeradmin")]
    public async Task WhenSomethingCrashes_NeverRevealsTheErrorDetails(string path)
    {
        var context = CreateContext(path);
        var middleware = new ExceptionHandlingMiddleware(Crash, _logger);

        await middleware.InvokeAsync(context);

        Assert.DoesNotContain("Secret connection string", await ReadBodyAsync(context));
    }

    [Fact]
    public async Task WhenSomethingCrashes_LogsTheFullErrorForDevelopers()
    {
        var context = CreateContext("/");
        var middleware = new ExceptionHandlingMiddleware(Crash, _logger);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.IsType<InvalidOperationException>(entry.Exception);
    }
}

/// <summary>Tests for the request logger.</summary>
public class RequestLoggingMiddlewareTests
{
    private readonly TestLogger<RequestLoggingMiddleware> _logger = new();

    private static DefaultHttpContext CreateContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        return context;
    }

    private static RequestDelegate RespondWith(int statusCode)
    {
        return ctx =>
        {
            ctx.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        };
    }

    [Fact]
    public async Task SuccessfulRequest_IsLoggedAsInformationWithDetails()
    {
        var context = CreateContext("GET", "/Community/ViewBlog/1");
        var middleware = new RequestLoggingMiddleware(RespondWith(200), _logger);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("GET /Community/ViewBlog/1 responded 200", entry.Message);
        Assert.Contains("user: anonymous", entry.Message);
    }

    [Fact]
    public async Task LoggedInUser_IsNamedInTheLog()
    {
        var context = CreateContext("POST", "/Post/Create");
        context.User = TestHelpers.LoggedInUser("author");
        var middleware = new RequestLoggingMiddleware(RespondWith(302), _logger);

        await middleware.InvokeAsync(context);

        Assert.Contains($"user: {TestHelpers.UserEmail}", Assert.Single(_logger.Entries).Message);
    }

    [Theory]
    [InlineData(404, LogLevel.Warning)]
    [InlineData(403, LogLevel.Warning)]
    [InlineData(500, LogLevel.Error)]
    [InlineData(302, LogLevel.Information)]
    public async Task LogLevel_MatchesHowSeriousTheResponseIs(int statusCode, LogLevel expectedLevel)
    {
        var context = CreateContext("GET", "/somewhere");
        var middleware = new RequestLoggingMiddleware(RespondWith(statusCode), _logger);

        await middleware.InvokeAsync(context);

        Assert.Equal(expectedLevel, Assert.Single(_logger.Entries).Level);
    }

    [Fact]
    public async Task WhenRequestCrashes_StillLogsAndLetsErrorContinue()
    {
        var context = CreateContext("GET", "/broken");
        var middleware = new RequestLoggingMiddleware(_ => throw new InvalidOperationException("Boom"), _logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        Assert.Contains("GET /broken", Assert.Single(_logger.Entries).Message);
    }
}
