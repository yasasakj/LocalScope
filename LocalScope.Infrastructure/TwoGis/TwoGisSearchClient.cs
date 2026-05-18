using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;
using Microsoft.Extensions.Configuration;

namespace LocalScope.Infrastructure.TwoGis;

public class TwoGisSearchClient : IPlacesProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string BaseUrl = "https://catalog.api.2gis.com";

    public TwoGisSearchClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["TwoGis:ApiKey"] 
            ?? throw new InvalidOperationException("API-ключ 2ГИС не настроен.");
        
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
        var text = string.IsNullOrWhiteSpace(category) ? "аптека" : category;
        
        // 2GIS API ограничивает page_size от 1 до 10
        var pageSize = Math.Min(limit, 10);
        
        string lonStr = center.Longitude.ToString("F6", CultureInfo.InvariantCulture);
        string latStr = center.Latitude.ToString("F6", CultureInfo.InvariantCulture);
        var pointParam = $"{lonStr},{latStr}";

        var url = $"3.0/items" +
                  $"?q={Uri.EscapeDataString(text)}" +
                  $"&point={Uri.EscapeDataString(pointParam)}" +
                  $"&radius={radiusMeters}" +
                  $"&sort=distance" +
                  $"&page_size={pageSize}" +
                  $"&key={_apiKey}" +
                  $"&fields=items.point,items.name,items.address_name,items.address_comment," +
                  $"items.contact_groups,items.schedule,items.reviews,items.rubrics";


        Console.WriteLine($"=== 2GIS REQUEST ===");
        Console.WriteLine($"Search: '{text}' at {pointParam}, radius={radiusMeters}m");
        Console.WriteLine($"URL: {BaseUrl}/{url.Replace(_apiKey, "***")}");

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            // Явно читаем байты и конвертируем в UTF-8
            var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var responseBody = Encoding.UTF8.GetString(responseBytes);
            
            Console.WriteLine($"=== 2GIS RESPONSE ===");
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Length: {responseBody.Length} bytes");
            
            if (responseBody.Length < 500)
            {
                Console.WriteLine($"Full body: {responseBody}");
            }
            else
            {
                Console.WriteLine($"First 500 chars: {responseBody[..500]}");
            }

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"ERROR: {response.StatusCode}");
                return Array.Empty<Place>();
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var data = JsonSerializer.Deserialize<TwoGisSearchResponse>(responseBody, options);

            if (data?.Meta?.Code != 200)
            {
                Console.WriteLine($"API Error: Code={data?.Meta?.Code}, Message={data?.Meta?.Error?.Message}");
                return Array.Empty<Place>();
            }

            var itemsCount = data?.Result?.Items?.Count ?? 0;
            var totalCount = data?.Result?.Total ?? 0;
            Console.WriteLine($"Items in response: {itemsCount}, Total available: {totalCount}");

            if (data?.Result?.Items == null || itemsCount == 0)
            {
                Console.WriteLine("No items found");
                return Array.Empty<Place>();
            }

            // Выводим первые 3 элемента
            Console.WriteLine("First 3 items:");
            foreach (var item in data.Result.Items.Take(3))
            {
                Console.WriteLine($"  - {item.Name} ({item.AddressName})");
            }

            var places = data.Result.Items
                .Select(item => MapToPlace(item, text))
                .Where(p => p != null)
                .Select(p => p!)
                .ToList();
            
            Console.WriteLine($"Successfully mapped: {places.Count} places");
            
            return places.Take(limit).ToList();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HTTP ERROR: {ex.Message}");
            return Array.Empty<Place>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON ERROR: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            return Array.Empty<Place>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UNEXPECTED ERROR: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            return Array.Empty<Place>();
        }
    }

    public async Task<Place?> GetPlaceDetailsAsync(
        string placeId, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(placeId)) return null;

        var url = $"3.0/items/byid?id={placeId}&key={_apiKey}" +
                  $"&fields=items.adm_div,items.contact_groups,items.schedule," +
                  $"items.reviews,items.rubrics";

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var responseBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var responseBody = Encoding.UTF8.GetString(responseBytes);

            if (!response.IsSuccessStatusCode)
                return null;

            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            
            var data = JsonSerializer.Deserialize<TwoGisSearchResponse>(responseBody, options);
            var item = data?.Result?.Items?.FirstOrDefault();
            return item != null ? MapToPlace(item, "организация") : null;
        }
        catch
        {
            return null;
        }
    }

    private static Place? MapToPlace(TwoGisItem item, string searchedCategory)
    {
        Console.WriteLine($"MapToPlace: ID={item.Id}, Name={item.Name}");
        
        if (item?.Point == null)
        {
            Console.WriteLine($"  -> SKIPPED: No coordinates for {item?.Name}");
            return null;
        }

        Console.WriteLine($"  -> Coordinates: Lat={item.Point.Lat}, Lon={item.Point.Lon}");

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

        var categoriesList = new List<string>();
        if (item.Rubrics != null && item.Rubrics.Any())
        {
            categoriesList.AddRange(item.Rubrics
                .Select(r => r.Name ?? "")
                .Where(name => !string.IsNullOrEmpty(name)));
        }
        
        if (!categoriesList.Contains(searchedCategory))
        {
            categoriesList.Add(searchedCategory);
        }

        Console.WriteLine($"  -> SUCCESS: Created place object");
        
        return new Place
        {
            Id = item.Id ?? Guid.NewGuid().ToString(),
            Name = item.Name ?? "Неизвестное место",
            Address = item.AddressName ?? item.AddressComment ?? "Адрес отсутствует",
            Location = location,
            Categories = categoriesList.ToArray(),
            Phone = phone,
            Website = website,
            Rating = item.Reviews?.GeneralRating,
            IsOpenNow = isOpenNow,
            Source = "2gis"
        };
    }
}

// --- МОДЕЛИ ДЕСЕРИАЛИЗАЦИИ ---
internal class TwoGisSearchResponse
{
    [JsonPropertyName("meta")]
    public TwoGisMeta? Meta { get; set; }
    
    [JsonPropertyName("result")]
    public TwoGisResult? Result { get; set; }
}

internal class TwoGisMeta
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("error")]
    public TwoGisError? Error { get; set; }
}

internal class TwoGisError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

internal class TwoGisResult
{
    [JsonPropertyName("items")]
    public List<TwoGisItem>? Items { get; set; }
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

internal class TwoGisItem
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }

    [JsonPropertyName("address_name")] 
    public string? AddressName { get; set; }      

    [JsonPropertyName("address_comment")] 
    public string? AddressComment { get; set; }   

    public TwoGisPoint? Point { get; set; }

    [JsonPropertyName("contact_groups")] 
    public List<TwoGisContactGroup>? ContactGroups { get; set; } 

    public TwoGisSchedule? Schedule { get; set; }
    public TwoGisReviews? Reviews { get; set; }
    public List<TwoGisRubric>? Rubrics { get; set; }
}

internal class TwoGisPoint
{
    [JsonPropertyName("lat")] 
    public double Lat { get; set; } 

    [JsonPropertyName("lon")] 
    public double Lon { get; set; }
}

internal class TwoGisContactGroup
{
    [JsonPropertyName("contacts")]
    public List<TwoGisContact>? Contacts { get; set; }
}

internal class TwoGisContact
{
    public string? Type { get; set; }
    public string? Value { get; set; }
}

internal class TwoGisSchedule
{
    [JsonPropertyName("is_opened")] 
    public object? IsOpened { get; set; }
}

internal class TwoGisReviews
{
    [JsonPropertyName("general_rating")] 
    public double? GeneralRating { get; set; }
}

internal class TwoGisRubric
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}