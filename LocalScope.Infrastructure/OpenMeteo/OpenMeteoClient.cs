using System.Net.Http.Json;
using System.Text.Json;
using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;

namespace LocalScope.Infrastructure.OpenMeteo;

public class OpenMeteoClient : IWeatherProvider
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.open-meteo.com/";

    public OpenMeteoClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }
    }

    public async Task<WeatherSnapshot?> GetCurrentWeatherAsync(GeoPoint location, CancellationToken cancellationToken = default)
    {
        var url = $"forecast?latitude={location.Latitude:F6}&longitude={location.Longitude:F6}" +
                  "&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,rain,cloud_cover,wind_speed_10m" +
                  "&timezone=auto";

        try
        {
            var data = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(url, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, cancellationToken);

            if (data?.Current == null)
                return GetDefaultFallbackWeather();

            return MapToWeatherSnapshot(data.Current, DateTime.UtcNow);
        }
        catch (Exception)
        {
            // Если сервер Open-Meteo упал (502, 504) или нет интернета, 
            // не роняем приложение, а возвращаем безопасную заглушку погоды
            return GetDefaultFallbackWeather();
        }
    }

    public async Task<IReadOnlyList<WeatherSnapshot>> GetForecastAsync(
        GeoPoint location,
        int days = 3,
        CancellationToken cancellationToken = default)
    {
        // Добавили слеш в начало пути запроса
        var url = $"forecast?latitude={location.Latitude:F6}&longitude={location.Longitude:F6}" +
                  $"&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max" +
                  $"&forecast_days={days}&timezone=auto";

        try
        {
            var data = await _httpClient.GetFromJsonAsync<OpenMeteoForecastResponse>(url, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, cancellationToken);

            if (data?.Daily == null || data.Daily.Time == null)
                return Array.Empty<WeatherSnapshot>();

            var forecasts = new List<WeatherSnapshot>();
            for (int i = 0; i < data.Daily.Time.Count && i < days; i++)
            {
                forecasts.Add(new WeatherSnapshot
                {
                    Timestamp = DateTime.TryParse(data.Daily.Time[i], out var date) ? date : DateTime.UtcNow,
                    TemperatureCelsius = (data.Daily.Temperature_2m_max[i] + data.Daily.Temperature_2m_min[i]) / 2,
                    WindSpeedMps = data.Daily.Wind_speed_10m_max[i],
                    PrecipitationMm = data.Daily.Precipitation_sum[i],
                    Condition = DetermineCondition(data.Daily.Precipitation_sum[i]),
                    Humidity = 0,
                    FeelsLikeCelsius = (data.Daily.Temperature_2m_max[i] + data.Daily.Temperature_2m_min[i]) / 2,
                    CloudCover = 0
                });
            }

            return forecasts;
        }
        catch
        {
            return Array.Empty<WeatherSnapshot>();
        }
    }
    private static WeatherSnapshot GetDefaultFallbackWeather()
    {
        return new WeatherSnapshot
        {
            Timestamp = DateTime.UtcNow,
            TemperatureCelsius = 0.0,
            FeelsLikeCelsius = 0.0,
            Humidity = 0,
            WindSpeedMps = 0.0,
            PrecipitationMm = 0.0,
            CloudCover = 0,
            Condition = "Сервер погоды временно недоступен"
        };
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
            PrecipitationMm = current.Precipitation ?? current.Rain ?? 0,
            CloudCover = current.Cloud_cover ?? 0,
            Condition = DetermineCondition(current.Precipitation ?? current.Rain ?? 0)
        };
    }

    private static string DetermineCondition(double precipitation)
    {
        return precipitation switch
        {
            > 5.0 => "Дождь",
            > 0.5 => "Небольшой дождь",
            _ => "Ясно"
        };
    }
}

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
