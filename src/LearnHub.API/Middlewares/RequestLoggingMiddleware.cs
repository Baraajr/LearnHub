using System.Diagnostics;
using System.Security.Claims;

namespace LearnHub.API.Middlewares;

public class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger
)
{
    private const string CorrelationHeader = "X-Correlation-ID";
    private const int SlowRequestMs = 1000;

    private static readonly string[] IgnoredPrefixes = ["/health", "/swagger", "/favicon.ico"];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;

        if (
            IgnoredPrefixes.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase))
        )
        {
            await next(context);
            return;
        }

        var correlationId =
            context.Request.Headers[CorrelationHeader].FirstOrDefault() ?? context.TraceIdentifier;

        context.Response.Headers[CorrelationHeader] = correlationId;

        using var scope = logger.BeginScope(
            new Dictionary<string, object> { ["CorrelationId"] = correlationId }
        );

        var start = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var failedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            logger.LogError(
                ex,
                "{Method} {Path} threw after {ElapsedMs:0}ms (User: {UserId})",
                context.Request.Method,
                path,
                failedMs,
                GetUserId(context)
            );

            throw;
        }

        var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var status = context.Response.StatusCode;

        var level = status switch
        {
            >= 500 => LogLevel.Error,
            >= 400 => LogLevel.Warning,
            _ when elapsedMs > SlowRequestMs => LogLevel.Warning,
            _ => LogLevel.Information,
        };

        logger.Log(
            level,
            "{Method} {Path} responded {StatusCode} in {ElapsedMs:0}ms (User: {UserId}, IP: {Ip}){Slow}",
            context.Request.Method,
            path,
            status,
            elapsedMs,
            GetUserId(context),
            context.Connection.RemoteIpAddress,
            elapsedMs > SlowRequestMs ? " [SLOW]" : ""
        );
    }

    // Read after next() so UseAuthentication has already populated context.User
    private static string GetUserId(HttpContext context) =>
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
}
