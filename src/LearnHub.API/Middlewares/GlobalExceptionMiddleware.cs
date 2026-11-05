using System.Text.Json;
using LearnHub.Services.Exceptions;

namespace LearnHub.API.Middlewares;

public class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IWebHostEnvironment environment
)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unhandled exception: {Method} {Path}",
                context.Request.Method,
                context.Request.Path
            );

            if (context.Response.HasStarted)
                throw;

            var isAppException = ex is AppException;

            var statusCode = isAppException
                ? ((AppException)ex).StatusCode
                : StatusCodes.Status500InternalServerError;

            var errorCode = isAppException ? ((AppException)ex).Code : "INTERNAL_SERVER_ERROR";

            var message =
                isAppException ? ex.Message
                : environment.IsDevelopment() ? ex.Message
                : "An unexpected error occurred.";

            var error = new
            {
                error = new
                {
                    code = errorCode,
                    message,
                    statusCode,
                    details = environment.IsDevelopment() ? ex.ToString() : null,
                },
            };

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
    }
}
