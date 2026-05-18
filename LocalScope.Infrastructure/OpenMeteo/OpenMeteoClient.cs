using System.Text.Json;
using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;

namespace LocalScope.Infrastructure.OpenMeteo;

/// <summary>
/// Клиент для работы с Open-Meteo API (погода)
/// </summary>
public class OpenMeteoClient : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.open-meteo.com/v1";

    public OpenMeteoClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<WeatherSnapshot?> GetCurrentWeatherAsync(GeoPoint location, CancellationToken cancellationToken = default)
    {
        var url = $"/forecast?latitude={location.Latitude:F6}&longitude={location.Longitude:F6}" +
                  "&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,rain,cloud_cover,wind_speed_10m" +
                  "&timezone=auto";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<OpenMeteoResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (data?.Current == null)
            return null;

        return MapToWeatherSnapshot(data.Current, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<WeatherSnapshot>> GetForecastAsync(
        GeoPoint location,
        int days = 3,
        CancellationToken cancellationToken = default)
    {
        var url = $"/forecast?latitude={location.Latitude:F6}&longitude={location.Longitude:F6}" +
                  $"&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max" +
                  $"&forecast_days={days}&timezone=auto";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<OpenMeteoForecastResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (data?.Daily == null)
            return Array.Empty<WeatherSnapshot>();

        var forecasts = new List<WeatherSnapshot>();
        for (int i = 0; i < data.Daily.Time.Count && i < days; i++)
        {
            forecasts.Add(new WeatherSnapshot
            {
                Timestamp = DateTime.Parse(data.Daily.Time[i]),
                TemperatureCelsius = (data.Daily.Temperature_2m_max[i] + data.Daily.Temperature_2m_min[i]) / 2,
                WindSpeedMps = data.Daily.Wind_speed_10m_max[i],
                PrecipitationMm = data.Daily.Precipitation_sum[i],
                Condition = DetermineCondition(data.Daily.Precipitation_sum[i]),
                Humidity = 0 // Not available in daily forecast
            });
        }

        return forecasts;
    }

    private static WeatherSnapshot MapToWeatherSnapshot(OpenMeteoCurrentData current, DateTime timestamp)
    {
        return new WeatherSnapshot
        {
            Timestamp = timestamp,
            TemperatureCelsius = current.Temperature_2m,
            FeelsLikeCelsius = current.Apparent_temperature,
            Humidity = current.Relative_humidity_2m,
            WindSpeedMps = current.Wind_speed_10m,
            PrecipitationMm = current.Precipitation ?? current.Rain,
            CloudCover = current.Cloud_cover,
            Condition = DetermineCondition(current.Precipitation ?? current.Rain ?? 0)
        };
    }

    private static string DetermineCondition(double precipitation)
    {
        return precipitation switch
        {
            > 5 => "rain",
            > 0.5 => "light_rain",
            _ => "clear"
        };
    }
}

// DTOs для десериализации ответов Open-Meteo
internal record OpenMeteoResponse(OpenMeteoCurrentData? Current);

internal record OpenMeteoCurrentData(
    double Temperature_2m,
    int Relative_humidity_2m,
    double Apparent_temperature,
    double? Precipitation,
    double? Rain,
    int? Cloud_cover,
    double Wind_speed_10m
);

internal record OpenMeteoForecastResponse(OpenMeteoDailyData? Daily);

internal record OpenMeteoDailyData(
    List<string> Time,
    List<double> Temperature_2m_max,
    List<double> Temperature_2m_min,
    List<double> Precipitation_sum,
    List<double> Wind_speed_10m_max
);