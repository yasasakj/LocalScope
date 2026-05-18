namespace LocalScope.Infrastructure.Yandex;
using System.Text.Json;
using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;
using Microsoft.Extensions.Configuration;

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
        _apiKey = configuration["Yandex:ApiKey"] ?? throw new InvalidOperationException("Yandex API key not configured");
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<GeocodingResult?> GeocodeAsync(string query, CancellationToken cancellationToken = default)
    {
        var url = $"?apikey={_apiKey}&geocode={Uri.EscapeDataString(query)}&format=json&results=1";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<YandexGeocodeResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var geoObject = data?.Response?.GeoObjectCollection?.FeatureMember?.FirstOrDefault()?.GeoObject;
        if (geoObject == null)
            return null;

        var coords = geoObject.Point.Pos.Split(' ');
        var lon = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
        var lat = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);

        return new GeocodingResult(
            new GeoPoint(lat, lon),
            geoObject.MetaDataProperty.GeocoderMetaData.Text,
            geoObject.MetaDataProperty.GeocoderMetaData.Precision.ToLowerInvariant()
        );
    }
}

// DTOs для Yandex Geocoder
internal record YandexGeocodeResponse(YandexResponse? Response);
internal record YandexResponse(YandexGeoObjectCollection? GeoObjectCollection);
internal record YandexGeoObjectCollection(List<YandexFeatureMember>? FeatureMember);
internal record YandexFeatureMember(YandexGeoObject GeoObject);
internal record YandexGeoObject(YandexPoint Point, YandexMetaDataProperty MetaDataProperty);
internal record YandexPoint(string Pos);
internal record YandexMetaDataProperty(YandexGeocoderMetaData GeocoderMetaData);
internal record YandexGeocoderMetaData(string Text, string Precision);
