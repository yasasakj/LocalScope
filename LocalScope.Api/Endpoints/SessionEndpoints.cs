using LocalScope.Shared.Contracts;

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
            .WithSummary("Создать новую сессию пользователя");
    }

    private static IResult CreateSession(SessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return Results.BadRequest(new { error = "DeviceId is required" });

        // В реальном приложении здесь была бы работа с БД
        var sessionToken = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddDays(30);

        var response = new SessionResponse
        {
            SessionToken = sessionToken,
            ExpiresAt = expiresAt
        };

        return Results.Ok(response);
    }
}
