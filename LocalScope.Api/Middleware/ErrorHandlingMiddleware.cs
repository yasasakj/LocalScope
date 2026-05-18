using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalScope.Api.Middleware;

/// <summary>
/// Middleware для централизованной обработки ошибок
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(
        RequestDelegate next, 
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment env) // Добавили проверку окружения (Dev/Prod)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Произошло необработанное исключение при запросе к {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Если это Prod, скрываем детали внутренних ошибок ради безопасности.
        // Если это Development, выводим сообщение как есть.
        string errorMessage = _env.IsDevelopment() 
            ? exception.Message 
            : "На сервере произошла внутренняя ошибка. Пожалуйста, повторите запрос позже.";

        var errorResponse = new
        {
            error = errorMessage,
            type = exception.GetType().Name
        };

        // Пишем JSON напрямую в поток ответа (Response Stream). 
        // Это гораздо быстрее и не засоряет память временными строками.
        await JsonSerializer.SerializeAsync(context.Response.Body, errorResponse);
    }
}

public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}
