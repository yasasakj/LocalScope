using System.Net.Http.Json;
using System.Text.Json;
using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace LocalScope.Infrastructure.Yandex;

/// <summary>
/// Клиент для поиска организаций через API Яндекса
/// </summary>
public class YandexSearchClient : IPlacesProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://search-maps.yandex.ru/v1/";

    public YandexSearchClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Yandex:SearchApiKey"] ?? throw new InvalidOperationException("API-ключ Поиска организаций Яндекса не настроен в appsettings.json (секция Yandex:SearchApiKey).");
        
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }
    }

    public async Task<IReadOnlyList<Place>> SearchNearbyAsync(
        GeoPoint center,
        string? category = null,
        int radiusMeters = 1000,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var text = category ?? "организация";
        
        // Переводим метры в приблизительный градусный радиус (spn) для более точного ограничения на стороне Яндекса
        double spnDelta = (radiusMeters / 111000.0) * 2.0;
        
        var url = $"?apikey={_apiKey}&text={Uri.EscapeDataString(text)}" +
                  $"&ll={center.Longitude:F6},{center.Latitude:F6}" +
                  $"&spn={spnDelta:F4},{spnDelta:F4}&type=biz&results={limit}&lang=ru_RU";

        try
        {
            // Высокопроизводительное потоковое чтение JSON из сети
            var data = await _httpClient.GetFromJsonAsync<YandexSearchResponse>(url, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }, cancellationToken);

            if (data?.Features == null)
                return Array.Empty<Place>();

            var places = data.Features
                .Select(f => MapToPlace(f, center))
                .Where(p => p != null && p.GetDistanceFrom(center) <= radiusMeters)
                .Select(p => p!)
                .Take(limit)
                .ToList();

            return places;
        }
        catch (Exception)
        {
            throw; // Пробрасываем ошибку в глобальное бэкенд-Middleware
        }
    }

    public async Task<Place?> GetPlaceDetailsAsync(string placeId, CancellationToken cancellationToken = default)
    {
        // Бесплатное API Поиска организаций Яндекса действительно не поддерживает прямые детальные запросы по ID.
        // Корректно возвращаем null, эндпоинт обработает это как Status404NotFound.
        return await Task.FromResult<Place?>(null);
    }

    private static Place? MapToPlace(YandexFeature feature, GeoPoint searchCenter)
    {
        // Защитная проверка: если структура координат повреждена, пропускаем эту организацию
        if (feature?.Geometry?.Coordinates == null || feature.Geometry.Coordinates.Count < 2)
            return null;

        var coords = feature.Geometry.Coordinates;
        var location = new GeoPoint(coords[1], coords[0]); // Порядок Яндекса: [долгота, широта]
        var props = feature.Properties?.CompanyMetaData;

        // Извлекаем статус работы (открыто/закрыто) из структуры Яндекса Hours
        bool? isOpenNow = null;
        if (props?.Hours?.State != null)
        {
            isOpenNow = props.Hours.State.ToLowerInvariant() == "open";
        }

        return new Place
        {
            Id = props?.Id ?? Guid.NewGuid().ToString(),
            Name = props?.Name ?? "Неизвестная организация",
            Address = props?.Address ?? "Адрес не указан",
            Location = location,
            Categories = props?.Categories?.Select(c => c.Name).ToArray() ?? Array.Empty<string>(),
            Phone = props?.Phones?.FirstOrDefault()?.Formatted,
            Website = props?.Url,
            Rating = props?.Score, // Карта свойств Яндекса: Score содержит оценку/рейтинг организации
            IsOpenNow = isOpenNow,
            Source = "yandex"
        };
    }
}

// Расширенные внутренние DTO для разбора полной структуры ответа Яндекса
internal record YandexSearchResponse(List<YandexFeature>? Features);
internal record YandexFeature(YandexGeometry? Geometry, YandexProperties? Properties);
internal record YandexGeometry(List<double>? Coordinates);
internal record YandexProperties(YandexCompanyMetaData? CompanyMetaData);
internal record YandexCompanyMetaData(
    string? Id,
    string? Name,
    string? Address,
    string? Url,
    double? Score, // Рейтинг организации (обычно от 0 до 5)
    YandexHours? Hours, // График работы
    List<YandexCategory>? Categories,
    List<YandexPhone>? Phones
);
internal record YandexHours(string? State, string? Text); // State может быть "open" или "closed"
internal record YandexCategory(string Name);
internal record YandexPhone(string Formatted);
