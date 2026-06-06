using System.Net;
using System.Text.Json;

namespace TwitterClone.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            InvalidOperationException  => (HttpStatusCode.Conflict,      exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized,  exception.Message),
            ArgumentException => (HttpStatusCode.BadRequest,     exception.Message),
            _ => (HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.")
        };

        _logger.LogError(exception, "Unhandled exception [{Type}]: {Message}", exception.GetType().Name, exception.Message);

        context.Response.StatusCode  = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new { errors = new[] { message } };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
