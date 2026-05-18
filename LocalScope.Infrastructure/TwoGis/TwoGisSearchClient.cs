using System.Net.Http.Json;
using System.Text.Json;
using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace LocalScope.Infrastructure.TwoGis;

/// <summary>
/// Клиент для поиска организаций через бесплатный демо-API 2ГИС (Справочник)
/// </summary>
public class TwoGisSearchClient : IPlacesProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://2gis.com";

    public TwoGisSearchClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["TwoGis:ApiKey"] ?? throw new InvalidOperationException("API-ключ 2ГИС не настроен в appsettings.json (секция TwoGis:ApiKey).");
        
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
        var text = string.IsNullOrWhiteSpace(category) ? "кафе" : category;
        
        // Формируем URL строго по спецификации API 2ГИС v3
        var url = $"/3.0/items?q={Uri.EscapeDataString(text)}" +
                  $"&point={center.Longitude:F6},{center.Latitude:F6}" +
                  $"&radius={radiusMeters}" +
                  $"&location={center.Longitude:F6},{center.Latitude:F6}" +
                  $"&sort=distance" +
                  $"&key={_apiKey}" +
                  $"&page_size={limit}&fields=items.adm_div,items.contact_groups,items.schedule,items.reviews";

        try
        {
            // Настраиваем JsonSerializerOptions для поддержки snake_case_lower из ответов 2ГИС
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            };

            var data = await _httpClient.GetFromJsonAsync<TwoGisSearchResponse>(url, options, cancellationToken);

            if (data?.Result?.Items == null)
                return Array.Empty<Place>();

            return data.Result.Items
                .Select(MapToPlace)
                .Where(p => p != null)
                .Select(p => p!)
                .ToList();
        }
        catch (HttpRequestException)
        {
            return Array.Empty<Place>();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<Place?> GetPlaceDetailsAsync(string placeId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(placeId)) return null;

        var url = $"/3.0/items/byid?id={placeId}&key={_apiKey}&fields=items.adm_div,items.contact_groups,items.schedule,items.reviews";

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            };

            var data = await _httpClient.GetFromJsonAsync<TwoGisSearchResponse>(url, options, cancellationToken);

            var item = data?.Result?.Items?.FirstOrDefault();
            return item != null ? MapToPlace(item) : null;
        }
        catch
        {
            return null;
        }
    }

    private static Place? MapToPlace(TwoGisItem item)
    {
        if (item?.Point == null || item.Point.Lat == 0 || item.Point.Lon == 0)
            return null;

        var location = new GeoPoint(item.Point.Lat, item.Point.Lon);

        string? phone = null;
        string? website = null;
        var contacts = item.ContactGroups?.FirstOrDefault()?.Contacts;
        if (contacts != null)
        {
            phone = contacts.FirstOrDefault(c => c.Type == "phone")?.Value;
            website = contacts.FirstOrDefault(c => c.Type == "website" || c.Type == "url")?.Value;
        }

        bool? isOpenNow = null;
        if (item.Schedule?.IsOpened is JsonElement jsonElement)
        {
            isOpenNow = jsonElement.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => jsonElement.GetInt32() == 1,
                _ => null
            };
        }
        else if (item.Schedule?.IsOpened is bool boolVal)
        {
            isOpenNow = boolVal;
        }
        else if (item.Schedule?.IsOpened is int intVal)
        {
            isOpenNow = intVal == 1;
        }

        return new Place
        {
            Id = item.Id ?? Guid.NewGuid().ToString(),
            Name = item.Name ?? "Неизвестное место",
            Address = item.AddressName ?? item.AddressComment ?? "Адрес отсутствует",
            Location = location,
            Categories = !string.IsNullOrEmpty(item.Type) ? new[] { item.Type } : Array.Empty<string>(),
            Phone = phone,
            Website = website,
            Rating = item.Reviews?.GeneralRating,
            IsOpenNow = isOpenNow,
            Source = "2gis"
        };
    }
}

// Внутренние DTO-модели, скорректированные под автоматический маппинг SnakeCaseLower
internal record TwoGisSearchResponse(TwoGisResult? Result);
internal record TwoGisResult(List<TwoGisItem>? Items);
internal record TwoGisItem(
    string? Id,
    string? Name,
    string? Type,
    string? AddressName,      
    string? AddressComment,   
    TwoGisPoint? Point,
    List<TwoGisContactGroup>? ContactGroups, 
    TwoGisSchedule? Schedule,
    TwoGisReviews? Reviews
);
internal record TwoGisPoint(double Lat, double Lon);
internal record TwoGisContactGroup(List<TwoGisContact>? Contacts);
internal record TwoGisContact(string? Type, string? Value);
internal record TwoGisSchedule(object? IsOpened); 
internal record TwoGisReviews(double? GeneralRating);
