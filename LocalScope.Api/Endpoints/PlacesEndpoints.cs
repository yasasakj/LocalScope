using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;
using LocalScope.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

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
        HttpContext context, // Переходим на прямое чтение контекста
        IPlacesProvider placesProvider,
        CancellationToken cancellationToken)
    {
        var latStr = context.Request.Query["lat"].ToString();
        var lonStr = context.Request.Query["lon"].ToString();
        var category = context.Request.Query["category"].ToString();
        var radiusStr = context.Request.Query["radius"].ToString();
        var openNowStr = context.Request.Query["openNow"].ToString();
        var limitStr = context.Request.Query["limit"].ToString();

// Универсальный парсер: заменяем точку на текущий системный разделитель (запятую или точку)
        string systemSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        latStr = latStr.Replace(".", systemSeparator).Replace(",", systemSeparator);
        lonStr = lonStr.Replace(".", systemSeparator).Replace(",", systemSeparator);

        if (!double.TryParse(latStr, NumberStyles.Any, CultureInfo.CurrentCulture, out double lat) ||
            !double.TryParse(lonStr, NumberStyles.Any, CultureInfo.CurrentCulture, out double lon))
        {
            return Results.BadRequest(new { error = "Некорректный формат координат lat/lon." });
        }

        int radius = int.TryParse(radiusStr, out var r) ? r : 1000;
        if (radius < 100 || radius > 5000)
            return Results.BadRequest(new { error = "Радиус должен быть в диапазоне от 100 до 5000 метров." });

        int limit = int.TryParse(limitStr, out var l) ? l : 20;
        if (limit < 1 || limit > 50)
            return Results.BadRequest(new { error = "Лимит количества мест должен быть от 1 до 50." });

        bool? openNow = bool.TryParse(openNowStr, out var o) ? o : null;

        // Создаем географическую точку
        var center = new GeoPoint(lat, lon);

        var places = await placesProvider.SearchNearbyAsync(center, string.IsNullOrWhiteSpace(category) ? null : category, radius, limit, cancellationToken);

        if (places == null)
            return Results.Ok(new PlacesNearbyResponse { Center = new LocationDto { Latitude = lat, Longitude = lon }, Items = new() });

        var filteredPlaces = openNow.HasValue
            ? places.Where(p => p.IsOpenNow == openNow.Value)
            : places;

        var response = new PlacesNearbyResponse
        {
            Center = new LocationDto { Latitude = lat, Longitude = lon },
            Category = string.IsNullOrWhiteSpace(category) ? null : category,
            RadiusMeters = radius,
            Items = filteredPlaces.Select(p => new PlaceSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Address = p.Address,
                Location = new LocationDto 
                { 
                    Latitude = p.Location.Latitude, 
                    Longitude = p.Location.Longitude 
                },
                DistanceMeters = Math.Round(p.GetDistanceFrom(center), 1),
                IsOpenNow = p.IsOpenNow,
                Rating = p.Rating,
                Categories = p.Categories,
                Score = Math.Round(p.CalculateScore(center, radius), 1),
                Source = p.Source
            }).ToList()
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> GetPlaceDetails(
        [FromRoute] string id,
        IPlacesProvider placesProvider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Results.BadRequest(new { error = "Идентификатор места не может быть пустым." });

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
}
