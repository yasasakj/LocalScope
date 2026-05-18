using LocalScope.Domain.Models;
using LocalScope.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace LocalScope.Api.Endpoints;

public static class FavoritesEndpoints
{
    // Хранилище в памяти (для учебного проекта)
    private static readonly List<FavoritePlace> Favorites = new();

    public static void MapFavoritesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/places/favorites")
            .WithTags("Favorites")
            .WithOpenApi();

        // POST - добавить в избранное
        group.MapPost("/", AddToFavorites)
            .WithName("AddToFavorites")
            .WithSummary("Добавить место в избранное")
            .Produces<FavoritePlaceResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest);

        // GET - получить все избранные
        group.MapGet("/", GetFavorites)
            .WithName("GetFavorites")
            .WithSummary("Получить список избранных мест")
            .Produces<FavoritePlacesListResponse>(StatusCodes.Status200OK);

        // DELETE - удалить из избранного
        group.MapDelete("/{id}", RemoveFromFavorites)
            .WithName("RemoveFromFavorites")
            .WithSummary("Удалить место из избранного")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static IResult AddToFavorites([FromBody] FavoritePlaceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PlaceId))
            return Results.BadRequest(new { error = "PlaceId обязателен." });

        // Проверка на дубликат
        if (Favorites.Any(f => f.PlaceId == request.PlaceId))
            return Results.BadRequest(new { error = "Это место уже в избранном." });

        var favorite = new FavoritePlace
        {
            Id = Guid.NewGuid().ToString(),
            PlaceId = request.PlaceId,
            Name = request.Name,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Category = request.Category,
            SavedAt = DateTime.UtcNow
        };

        Favorites.Add(favorite);

        var response = new FavoritePlaceResponse
        {
            Id = favorite.Id,
            PlaceId = favorite.PlaceId,
            Name = favorite.Name,
            Address = favorite.Address,
            Latitude = favorite.Latitude,
            Longitude = favorite.Longitude,
            Category = favorite.Category,
            SavedAt = favorite.SavedAt
        };

        return Results.Created($"/api/v1/places/favorites/{favorite.Id}", response);
    }

    private static IResult GetFavorites()
    {
        var response = new FavoritePlacesListResponse
        {
            Favorites = Favorites.Select(f => new FavoritePlaceResponse
            {
                Id = f.Id,
                PlaceId = f.PlaceId,
                Name = f.Name,
                Address = f.Address,
                Latitude = f.Latitude,
                Longitude = f.Longitude,
                Category = f.Category,
                SavedAt = f.SavedAt
            }).ToList()
        };

        return Results.Ok(response);
    }

    private static IResult RemoveFromFavorites([FromRoute] string id)
    {
        var favorite = Favorites.FirstOrDefault(f => f.Id == id);
        if (favorite == null)
            return Results.NotFound(new { error = "Избранное место не найдено." });

        Favorites.Remove(favorite);
        return Results.Ok(new { message = "Место удалено из избранного." });
    }
}