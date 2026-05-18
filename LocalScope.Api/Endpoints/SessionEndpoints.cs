using LocalScope.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace LocalScope.Api.Endpoints;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/sessions")
            .WithTags("Sessions")
            .WithOpenApi();

        group.MapPost("/", CreateSession)
            .WithName("CreateSession")
            .WithSummary("Создать новую сессию пользователя")
            .Produces<SessionResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);
    }

    private static IResult CreateSession([FromBody] SessionRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.DeviceId))
            return Results.BadRequest(new { error = "Параметр 'DeviceId' обязателен для заполнения." });

        // Генерация временного токена (в будущем здесь будет вызов БД или кэша)
        var sessionToken = Guid.NewGuid().ToString("D"); // "D" гарантирует стандартный формат с дефисами
        var expiresAt = DateTime.UtcNow.AddDays(30);

        var response = new SessionResponse
        {
            SessionToken = sessionToken,
            ExpiresAt = expiresAt
        };

        return Results.Ok(response);
    }
}