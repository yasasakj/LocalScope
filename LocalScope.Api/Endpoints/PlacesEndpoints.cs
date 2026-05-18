using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;
using LocalScope.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace LocalScope.Api.Endpoints;

public static class PlacesEndpoints
{
    public static void MapPlacesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/places")
            .WithTags("Places")
            .WithOpenApi();

        group.MapGet("/nearby", SearchNearby)
            .WithName("SearchNearby")
            .WithSummary("Найти места рядом")
            .Produces<PlacesNearbyResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapGet("/{id}", GetPlaceDetails)
            .WithName("GetPlaceDetails")
            .WithSummary("Получить детали места")
            .Produces<PlaceDetailsDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> SearchNearby(
    [FromQuery(Name = "lat")] double? latParam,        // Старый параметр
    [FromQuery(Name = "lon")] double? lonParam,        // Старый параметр
    [FromQuery(Name = "latitude")] double? latitudeParam, // Новый параметр
    [FromQuery(Name = "longitude")] double? longitudeParam, // Новый параметр
    [FromQuery(Name = "category")] string? category,
    [FromQuery(Name = "radius")] int radiusMeters = 1000,
    [FromQuery(Name = "limit")] int limit = 10,  // Изменено на 10 (ограничение 2GIS)
    [FromQuery(Name = "openNow")] bool? openNow = null,
    IPlacesProvider placesProvider = null!,
    CancellationToken cancellationToken = default)
{
    // Определяем какие параметры использовать (приоритет новым)
    double latitude = latitudeParam ?? latParam ?? 0;
    double longitude = longitudeParam ?? lonParam ?? 0;
    
    // Если координаты не указаны
    if (latitude == 0 && longitude == 0)
        return Results.BadRequest(new { error = "Не указаны координаты (latitude/longitude или lat/lon)." });
    
    // Валидация координат
    if (latitude < -90 || latitude > 90)
        return Results.BadRequest(new { error = "Широта должна быть от -90 до 90 градусов." });
    
    if (longitude < -180 || longitude > 180)
        return Results.BadRequest(new { error = "Долгота должна быть от -180 до 180 градусов." });

    // Валидация радиуса
    if (radiusMeters < 100 || radiusMeters > 5000)
        return Results.BadRequest(new { error = "Радиус должен быть в диапазоне от 100 до 5000 метров." });

    // Валидация лимита (учёт ограничения 2GIS API)
    if (limit < 1 || limit > 20)
        return Results.BadRequest(new { error = "Лимит количества мест должен быть от 1 до 20." });

    var center = new GeoPoint(latitude, longitude);

    try
    {
        var places = await placesProvider.SearchNearbyAsync(
            center, 
            category: category, 
            radiusMeters: radiusMeters, 
            limit: limit, 
            cancellationToken: cancellationToken);

        var placesList = places ?? Array.Empty<Place>();

        var filteredPlaces = openNow.HasValue
            ? placesList.Where(p => p.IsOpenNow == openNow.Value)
            : placesList;

        var response = new PlacesNearbyResponse
        {
            Center = new LocationDto 
            { 
                Latitude = latitude, 
                Longitude = longitude 
            },
            Category = category,
            RadiusMeters = radiusMeters,
            Items = filteredPlaces.Select(p =>
            {
                var distance = p.GetDistanceFrom(center);
                var score = p.CalculateScore(center, radiusMeters);
                
                return new PlaceSummaryDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Address = p.Address,
                    Location = new LocationDto 
                    { 
                        Latitude = p.Location.Latitude, 
                        Longitude = p.Location.Longitude 
                    },
                    DistanceMeters = Math.Round(distance, 1),
                    IsOpenNow = p.IsOpenNow,
                    Rating = p.Rating,
                    Categories = p.Categories,
                    Score = double.IsNaN(score) ? null : Math.Round(score, 1),
                    Source = p.Source
                };
            }).ToList()
        };

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR in SearchNearby: {ex.Message}");
        Console.WriteLine($"Stack: {ex.StackTrace}");
        
        return Results.Problem(
            detail: "Произошла ошибка при поиске мест. Пожалуйста, попробуйте позже.",
            statusCode: 500);
    }
}

    private static async Task<IResult> GetPlaceDetails(
        [FromRoute] string id,
        IPlacesProvider placesProvider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Results.BadRequest(new { error = "Идентификатор места не может быть пустым." });

        try
        {
            var place = await placesProvider.GetPlaceDetailsAsync(id, cancellationToken);

            if (place == null)
                return Results.NotFound(new { error = $"Место с ID '{id}' не найдено." });

            var response = new PlaceDetailsDto
            {
                Id = place.Id,
                Name = place.Name,
                Address = place.Address,
                Location = new LocationDto 
                { 
                    Latitude = place.Location.Latitude, 
                    Longitude = place.Location.Longitude 
                },
                Categories = place.Categories,
                IsOpenNow = place.IsOpenNow,
                Phone = place.Phone,
                Website = place.Website,
                Rating = place.Rating,
                Description = place.Description,
                Source = place.Source
            };

            return Results.Ok(response);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting place details: {ex.Message}");
            
            return Results.Problem(
                detail: "Произошла ошибка при получении информации о месте.",
                statusCode: 500);
        }
    }
}