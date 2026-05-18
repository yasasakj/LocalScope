using LocalScope.Domain.Abstractions;
using LocalScope.Shared.Contracts;

namespace LocalScope.Api.Endpoints;

public static class GeocodeEndpoints
{
    public static void MapGeocodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/geocode")
            .WithTags("Geocoding")
            .WithOpenApi();

        group.MapGet("/", Geocode)
            .WithName("Geocode")
            .WithSummary("Преобразовать адрес в координаты");
    }

    private static async Task<IResult> Geocode(
        string query,
        IGeocodingProvider geocodingProvider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Results.BadRequest(new { error = "Query parameter is required" });

        var result = await geocodingProvider.GeocodeAsync(query, cancellationToken);

        if (result == null)
            return Results.NotFound(new { error = "Location not found" });

        var response = new GeocodeResponse
        {
            FormattedAddress = result.FormattedAddress,
            Latitude = result.Location.Latitude,
            Longitude = result.Location.Longitude,
            Precision = result.Precision
        };

        return Results.Ok(response);
    }
}
