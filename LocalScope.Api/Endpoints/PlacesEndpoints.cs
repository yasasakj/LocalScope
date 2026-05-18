using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;
using LocalScope.Shared.Contracts;

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
            .WithSummary("Найти места рядом");

        group.MapGet("/{id}", GetPlaceDetails)
            .WithName("GetPlaceDetails")
            .WithSummary("Получить детали места");
    }

    private static async Task<IResult> SearchNearby(
        double lat,
        double lon,
        string? category,
        int radius,
        bool? openNow,
        int limit,
        IPlacesProvider placesProvider,
        CancellationToken cancellationToken)
    {
        if (radius < 100 || radius > 5000)
            return Results.BadRequest(new { error = "Radius must be between 100 and 5000 meters" });

        if (limit < 1 || limit > 50)
            return Results.BadRequest(new { error = "Limit must be between 1 and 50" });

        var center = new GeoPoint(lat, lon);
        var places = await placesProvider.SearchNearbyAsync(center, category, radius, limit, cancellationToken);

        // Фильтрация по openNow если указано
        var filteredPlaces = openNow.HasValue
            ? places.Where(p => p.IsOpenNow == openNow.Value).ToList()
            : places.ToList();

        var response = new PlacesNearbyResponse
        {
            Center = new LocationDto { Latitude = lat, Longitude = lon },
            Category = category,
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
                DistanceMeters = Math.Round((double)p.GetDistanceFrom(center), 2),
                IsOpenNow = p.IsOpenNow,
                Rating = p.Rating,
                Categories = p.Categories,
                Score = Math.Round((double)p.CalculateScore(center, radius), 2),
                Source = p.Source
            }).ToList()
        };

        return Results.Ok(response);
    }

    private static async Task<IResult> GetPlaceDetails(
        string id,
        IPlacesProvider placesProvider,
        CancellationToken cancellationToken)
    {
        var place = await placesProvider.GetPlaceDetailsAsync(id, cancellationToken);

        if (place == null)
            return Results.NotFound(new { error = "Place not found" });

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