using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    public async Task<WeatherSnapshot?> GetCurrentWeatherAsync(
        GeoPoint location, 
        CancellationToken cancellationToken = default)
    {
        var url = $"forecast" +  // ← Оставить v1/ так как BaseUrl без v1
                  $"?latitude={location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"&longitude={location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,rain,cloud_cover,wind_speed_10m,weather_code" +
                  $"&timezone=auto";

        Console.WriteLine($"OpenMeteo URL: {BaseUrl}{url}");

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            Console.WriteLine($"OpenMeteo Status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"OpenMeteo Error: {responseBody}");
                return GetDefaultFallbackWeather();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var data = JsonSerializer.Deserialize<OpenMeteoResponse>(responseBody, options);

            if (data?.Current == null)
            {
                Console.WriteLine("OpenMeteo: No current weather data");
                return GetDefaultFallbackWeather();
            }

            Console.WriteLine($"OpenMeteo: Temp={data.Current.Temperature2m}°C, Wind={data.Current.WindSpeed10m}m/s");

            return MapToWeatherSnapshot(data.Current, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OpenMeteo Error: {ex.Message}");
            return GetDefaultFallbackWeather();
        }
    }

    public async Task<IReadOnlyList<WeatherSnapshot>> GetForecastAsync(
        GeoPoint location,
        int days = 3,
        CancellationToken cancellationToken = default)
    {
        var url = $"forecast" +
                  $"?latitude={location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"&longitude={location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,wind_speed_10m_max,weather_code" +
                  $"&forecast_days={days}" +
                  $"&timezone=auto";

        Console.WriteLine($"OpenMeteo Forecast URL: {BaseUrl}{url}");

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"OpenMeteo Forecast Error: {responseBody}");
                return Array.Empty<WeatherSnapshot>();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var data = JsonSerializer.Deserialize<OpenMeteoForecastResponse>(responseBody, options);

            if (data?.Daily?.Time == null || data.Daily.Time.Count == 0)
            {
                Console.WriteLine("OpenMeteo: No forecast data");
                return Array.Empty<WeatherSnapshot>();
            }

            var forecasts = new List<WeatherSnapshot>();
            for (int i = 0; i < data.Daily.Time.Count && i < days; i++)
            {
                var weatherCode = i < (data.Daily.WeatherCode?.Count ?? 0) 
                    ? data.Daily.WeatherCode![i] 
                    : 0;

                forecasts.Add(new WeatherSnapshot
                {
                    Timestamp = DateTime.TryParse(data.Daily.Time[i], out var date) 
                        ? date 
                        : DateTime.UtcNow.AddDays(i),
                    TemperatureCelsius = Math.Round((data.Daily.Temperature2mMax[i] + data.Daily.Temperature2mMin[i]) / 2, 1),
                    FeelsLikeCelsius = Math.Round((data.Daily.Temperature2mMax[i] + data.Daily.Temperature2mMin[i]) / 2, 1),
                    Humidity = 0,
                    WindSpeedMps = data.Daily.WindSpeed10mMax[i],
                    PrecipitationMm = data.Daily.PrecipitationSum[i],
                    CloudCover = null,
                    Pressure = null,
                    Condition = GetWeatherDescription(weatherCode)
                });
            }

            Console.WriteLine($"OpenMeteo Forecast: {forecasts.Count} days");
            return forecasts;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OpenMeteo Forecast Error: {ex.Message}");
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
            Pressure = null,
            Condition = "Сервер погоды временно недоступен"
        };
    }

    private static WeatherSnapshot MapToWeatherSnapshot(OpenMeteoCurrentData current, DateTime timestamp)
    {
        return new WeatherSnapshot
        {
            Timestamp = timestamp,
            TemperatureCelsius = current.Temperature2m,
            FeelsLikeCelsius = current.ApparentTemperature,
            Humidity = current.RelativeHumidity2m,
            WindSpeedMps = current.WindSpeed10m,
            PrecipitationMm = current.Precipitation ?? current.Rain ?? 0,
            CloudCover = current.CloudCover ?? 0,
            Pressure = null,
            Condition = GetWeatherDescription(current.WeatherCode ?? 0)
        };
    }

    private static string GetWeatherDescription(int weatherCode)
    {
        return weatherCode switch
        {
            0 => "Ясно",
            1 => "Преимущественно ясно",
            2 => "Переменная облачность",
            3 => "Пасмурно",
            45 or 48 => "Туман",
            51 => "Легкая морось",
            53 => "Морось",
            55 => "Сильная морось",
            61 => "Небольшой дождь",
            63 => "Дождь",
            65 => "Сильный дождь",
            71 => "Небольшой снег",
            73 => "Снег",
            75 => "Сильный снег",
            77 => "Снежные зерна",
            80 => "Ливень",
            81 => "Сильный ливень",
            82 => "Очень сильный ливень",
            85 => "Небольшой снегопад",
            86 => "Сильный снегопад",
            95 => "Гроза",
            96 or 99 => "Гроза с градом",
            _ => "Ясно"
        };
    }
}

// --- МОДЕЛИ ОТВЕТА ---
internal record OpenMeteoResponse(
    [property: JsonPropertyName("current")] OpenMeteoCurrentData? Current
);

internal record OpenMeteoCurrentData(
    [property: JsonPropertyName("temperature_2m")] double Temperature2m,
    [property: JsonPropertyName("relative_humidity_2m")] int RelativeHumidity2m,
    [property: JsonPropertyName("apparent_temperature")] double ApparentTemperature,
    [property: JsonPropertyName("precipitation")] double? Precipitation,
    [property: JsonPropertyName("rain")] double? Rain,
    [property: JsonPropertyName("cloud_cover")] int? CloudCover,
    [property: JsonPropertyName("wind_speed_10m")] double WindSpeed10m,
    [property: JsonPropertyName("weather_code")] int? WeatherCode
);

internal record OpenMeteoForecastResponse(
    [property: JsonPropertyName("daily")] OpenMeteoDailyData? Daily
);

internal record OpenMeteoDailyData(
    [property: JsonPropertyName("time")] List<string> Time,
    [property: JsonPropertyName("temperature_2m_max")] List<double> Temperature2mMax,
    [property: JsonPropertyName("temperature_2m_min")] List<double> Temperature2mMin,
    [property: JsonPropertyName("precipitation_sum")] List<double> PrecipitationSum,
    [property: JsonPropertyName("wind_speed_10m_max")] List<double> WindSpeed10mMax,
    [property: JsonPropertyName("weather_code")] List<int>? WeatherCode
);