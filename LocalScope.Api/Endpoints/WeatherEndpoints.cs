using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;
using LocalScope.Shared.Contracts;

namespace LocalScope.Api.Endpoints;

public static class WeatherEndpoints
{
    public static void MapWeatherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/weather")
            .WithTags("Weather")
            .WithOpenApi();

        group.MapGet("/", GetWeather)
            .WithName("GetWeather")
            .WithSummary("Получить погоду и прогноз");
    }

    private static async Task<IResult> GetWeather(
        double lat,
        double lon,
        int days,
        IWeatherProvider weatherProvider,
        CancellationToken cancellationToken)
    {
        if (days < 1 || days > 7)
            return Results.BadRequest(new { error = "Days must be between 1 and 7" });

        var location = new GeoPoint(lat, lon);

        var currentTask = weatherProvider.GetCurrentWeatherAsync(location, cancellationToken);
        var forecastTask = weatherProvider.GetForecastAsync(location, days, cancellationToken);

        await Task.WhenAll(currentTask, forecastTask);

        var current = await currentTask;
        var forecast = await forecastTask;

        if (current == null)
            return Results.NotFound(new { error = "Weather data not available" });

        var response = new WeatherResponse
        {
            Now = MapToDto(current),
            Forecast = forecast.Select(MapToDto).ToList()
        };

        return Results.Ok(response);
    }

    private static WeatherSnapshotDto MapToDto(WeatherSnapshot snapshot)
    {
        return new WeatherSnapshotDto
        {
            Timestamp = snapshot.Timestamp,
            TemperatureCelsius = snapshot.TemperatureCelsius,
            FeelsLikeCelsius = snapshot.FeelsLikeCelsius,
            Humidity = snapshot.Humidity,
            WindSpeedMps = snapshot.WindSpeedMps,
            PrecipitationMm = snapshot.PrecipitationMm,
            Condition = snapshot.Condition,
            CloudCover = snapshot.CloudCover,
            ComfortScore = snapshot.GetComfortScore(),
            ComfortDescription = snapshot.GetComfortDescription()
        };
    }
}