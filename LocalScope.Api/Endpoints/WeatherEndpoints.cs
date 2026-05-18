using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;
using LocalScope.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

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
        HttpContext context, // Читаем строку запроса напрямую из контекста
        IWeatherProvider weatherProvider,
        CancellationToken cancellationToken)
    {
        // Принудительно вытаскиваем "lat" и "lon" по их текстовым именам
        var latStr = context.Request.Query["lat"].ToString();
        var lonStr = context.Request.Query["lon"].ToString();
        var daysStr = context.Request.Query["days"].ToString();

// Универсальный парсер: заменяем точку на текущий системный разделитель (запятую или точку)
        string systemSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        latStr = latStr.Replace(".", systemSeparator).Replace(",", systemSeparator);
        lonStr = lonStr.Replace(".", systemSeparator).Replace(",", systemSeparator);

        if (!double.TryParse(latStr, NumberStyles.Any, CultureInfo.CurrentCulture, out double lat) ||
            !double.TryParse(lonStr, NumberStyles.Any, CultureInfo.CurrentCulture, out double lon))
        {
            return Results.BadRequest(new { error = "Некорректный формат координат lat/lon." });
        }

        int days = int.TryParse(daysStr, out var d) ? d : 3;
        if (days < 1 || days > 7)
            return Results.BadRequest(new { error = "Количество дней в прогнозе должно быть от 1 до 7." });

        // Теперь параметры ЖЕСТКО привязаны к своим именам из URL
        var location = new GeoPoint(lat, lon);

        var currentTask = weatherProvider.GetCurrentWeatherAsync(location, cancellationToken);
        var forecastTask = weatherProvider.GetForecastAsync(location, days, cancellationToken);

        await Task.WhenAll(currentTask, forecastTask);

        var current = await currentTask;
        var forecast = await forecastTask;

        if (current == null)
            return Results.NotFound(new { error = "Данные о погоде сейчас недоступны." });

        var response = new WeatherDto
        {
            Timestamp = current.Timestamp,
            TemperatureCelsius = current.TemperatureCelsius,
            FeelsLikeCelsius = current.FeelsLikeCelsius,
            Humidity = current.Humidity,
            WindSpeedMps = current.WindSpeedMps,
            PrecipitationMm = current.PrecipitationMm,
            Condition = current.Condition,
            CloudCover = current.CloudCover,
            ComfortScore = current.GetComfortScore(),
            ComfortDescription = current.GetComfortDescription(),
            Forecast = forecast?.Select(f => new WeatherSnapshotDto
            {
                Timestamp = f.Timestamp,
                TemperatureCelsius = f.TemperatureCelsius,
                FeelsLikeCelsius = f.FeelsLikeCelsius,
                Humidity = f.Humidity,
                WindSpeedMps = f.WindSpeedMps,
                PrecipitationMm = f.PrecipitationMm,
                Condition = f.Condition,
                CloudCover = f.CloudCover,
                ComfortScore = f.GetComfortScore(),
                ComfortDescription = f.GetComfortDescription()
            }).ToList() ?? new List<WeatherSnapshotDto>()
        };

        return Results.Ok(response);
    }
}