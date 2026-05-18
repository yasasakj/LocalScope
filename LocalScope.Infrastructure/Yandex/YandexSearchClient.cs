namespace LocalScope.Infrastructure.Yandex;
using System.Text.Json;
using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;
using Microsoft.Extensions.Configuration;


/// <summary>
/// Клиент для поиска организаций через Яндекс API
/// </summary>
public class YandexSearchClient : IPlacesProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://search-maps.yandex.ru/v1/";

    public YandexSearchClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Yandex:SearchApiKey"] ?? throw new InvalidOperationException("Yandex Search API key not configured");
        _httpClient.BaseAddress = new Uri(BaseUrl);
    }

    public async Task<IReadOnlyList<Place>> SearchNearbyAsync(
        GeoPoint center,
        string? category = null,
        int radiusMeters = 1000,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var text = category ?? "организация";
        var url = $"?apikey={_apiKey}&text={Uri.EscapeDataString(text)}" +
                  $"&ll={center.Longitude:F6},{center.Latitude:F6}" +
                  $"&spn=0.01,0.01&type=biz&results={limit}&lang=ru_RU";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<YandexSearchResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (data?.Features == null)
            return Array.Empty<Place>();

        var places = data.Features
            .Select(f => MapToPlace(f, center))
            .Where(p => p.GetDistanceFrom(center) <= radiusMeters)
            .Take(limit)
            .ToList();

        return places;
    }

    public async Task<Place?> GetPlaceDetailsAsync(string placeId, CancellationToken cancellationToken = default)
    {
        // Yandex Search API не поддерживает получение по ID напрямую
        // Возвращаем null или можно реализовать поиск по названию
        await Task.CompletedTask;
        return null;
    }

    private static Place MapToPlace(YandexFeature feature, GeoPoint searchCenter)
    {
        var coords = feature.Geometry.Coordinates;
        var location = new GeoPoint(coords[1], coords[0]);
        var props = feature.Properties.CompanyMetaData;

        return new Place
        {
            Id = feature.Properties.CompanyMetaData?.Id ?? Guid.NewGuid().ToString(),
            Name = props?.Name ?? "Неизвестно",
            Address = props?.Address ?? "",
            Location = location,
            Categories = props?.Categories?.Select(c => c.Name).ToArray(),
            Phone = props?.Phones?.FirstOrDefault()?.Formatted,
            Website = props?.Url,
            Source = "yandex"
        };
    }
}

// DTOs для Yandex Search
internal record YandexSearchResponse(List<YandexFeature>? Features);
internal record YandexFeature(YandexGeometry Geometry, YandexProperties Properties);
internal record YandexGeometry(List<double> Coordinates);
internal record YandexProperties(YandexCompanyMetaData? CompanyMetaData);
internal record YandexCompanyMetaData(
    string? Id,
    string? Name,
    string? Address,
    string? Url,
    List<YandexCategory>? Categories,
    List<YandexPhone>? Phones
);
internal record YandexCategory(string Name);
internal record YandexPhone(string Formatted);
