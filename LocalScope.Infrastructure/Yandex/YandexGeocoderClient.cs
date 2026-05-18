using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace LocalScope.Infrastructure.Yandex;

/// <summary>
/// Клиент для Яндекс Геокодер API
/// </summary>
public class YandexGeocoderClient : IGeocodingProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://geocode-maps.yandex.ru/1.x/";

    public YandexGeocoderClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Yandex:ApiKey"] ?? throw new InvalidOperationException("API-ключ Яндекса не настроен в конфигурационном файле appsettings.json (секция Yandex:ApiKey).");
        
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }
    }

    public async Task<GeocodingResult?> GeocodeAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        var url = $"?apikey={_apiKey}&geocode={Uri.EscapeDataString(query)}&format=json&results=1";

        try
        {
            // Читаем напрямую из сети без промежуточного выделения строк в памяти
            var data = await _httpClient.GetFromJsonAsync<YandexGeocodeResponse>(url, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, cancellationToken);

            var geoObject = data?.Response?.GeoObjectCollection?.FeatureMember?.FirstOrDefault()?.GeoObject;
            if (geoObject?.Point?.Pos == null || geoObject.MetaDataProperty?.GeocoderMetaData == null)
                return null;

            var coords = geoObject.Point.Pos.Split(' ');
            if (coords.Length < 2)
                return null;

            // В API Яндекса порядок: [0] - ДОЛГОТА, [1] - ШИРОТА
            if (!double.TryParse(coords[0], CultureInfo.InvariantCulture, out var lon) ||
                !double.TryParse(coords[1], CultureInfo.InvariantCulture, out var lat))
            {
                return null;
            }

            // Защитная валидация: если координаты выходят за рамки допустимых доменных правил GeoPoint
            if (lat < -90 || lat > 90 || lon < -180 || lon > 180)
                return null;

            return new GeocodingResult(
                new GeoPoint(lat, lon),
                geoObject.MetaDataProperty.GeocoderMetaData.Text ?? query,
                geoObject.MetaDataProperty.GeocoderMetaData.Precision?.ToLowerInvariant() ?? "unknown"
            );
        }
        catch (Exception)
        {
            // Передаем исключение наверх, в Middleware бэкенда
            throw;
        }
    }
}

// Внутренние DTO для безопасной десериализации ответа Яндекса
internal record YandexGeocodeResponse(YandexResponse? Response);
internal record YandexResponse(YandexGeoObjectCollection? GeoObjectCollection);
internal record YandexGeoObjectCollection(List<YandexFeatureMember>? FeatureMember);
internal record YandexFeatureMember(YandexGeoObject? GeoObject);
internal record YandexGeoObject(YandexPoint? Point, YandexMetaDataProperty? MetaDataProperty);
internal record YandexPoint(string? Pos);
internal record YandexMetaDataProperty(YandexGeocoderMetaData? GeocoderMetaData);
internal record YandexGeocoderMetaData(string? Text, string? Precision);
