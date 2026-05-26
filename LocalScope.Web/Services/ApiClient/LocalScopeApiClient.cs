using System.Globalization;
using System.Net.Http.Json;
using LocalScope.Shared.Contracts;
using Microsoft.AspNetCore.WebUtilities;

namespace LocalScope.Web.Services.ApiClient;

public class LocalScopeApiClient
{
    private readonly HttpClient _httpClient;

    public LocalScopeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<GeocodeResponse?> GeocodeAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        try
        {
            // Автоматически определяет где запущен API
            var apiUrl = GetApiUrl();
            return await _httpClient.GetFromJsonAsync<GeocodeResponse>($"{apiUrl}/api/v1/geocode?query={Uri.EscapeDataString(query)}");
        }
        catch (HttpRequestException)
        {
            throw new Exception("Не удалось связаться с сервером геокодирования.");
        }
    }

    public async Task<WeatherResponse?> GetWeatherAsync(double lat, double lon, int days = 3)
    {
        var queryParams = new Dictionary<string, string?>
        {
            { "lat", lat.ToString("F6", CultureInfo.InvariantCulture) },
            { "lon", lon.ToString("F6", CultureInfo.InvariantCulture) },
            { "days", days.ToString() }
        };

        var url = QueryHelpers.AddQueryString("api/v1/weather", queryParams);
        var apiUrl = GetApiUrl();

        try
        {
            return await _httpClient.GetFromJsonAsync<WeatherResponse>($"{apiUrl}/{url}");
        }
        catch (HttpRequestException)
        {
            throw new Exception("Не удалось загрузить данные о погоде.");
        }
    }

    public async Task<PlacesNearbyResponse?> SearchPlacesAsync(double lat, double lon, string? category = null, int radius = 1000, int limit = 20)
    {
        var queryParams = new Dictionary<string, string?>
        {
            { "lat", lat.ToString("F6", CultureInfo.InvariantCulture) },
            { "lon", lon.ToString("F6", CultureInfo.InvariantCulture) },
            { "radius", radius.ToString() },
            { "limit", limit.ToString() }
        };

        if (!string.IsNullOrEmpty(category))
            queryParams.Add("category", category);

        var url = QueryHelpers.AddQueryString("api/v1/places/nearby", queryParams);
        var apiUrl = GetApiUrl();

        try
        {
            return await _httpClient.GetFromJsonAsync<PlacesNearbyResponse>($"{apiUrl}/{url}");
        }
        catch (HttpRequestException)
        {
            throw new Exception("Ошибка при получении списка заведений.");
        }
    }

    public async Task AddToFavoritesAsync(FavoritePlaceRequest request)
    {
        var apiUrl = GetApiUrl();
        var response = await _httpClient.PostAsJsonAsync($"{apiUrl}/api/v1/places/favorites", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PlaceDetailsDto?> GetPlaceDetailsAsync(string placeId)
    {
        if (string.IsNullOrWhiteSpace(placeId)) return null;

        try
        {
            var apiUrl = GetApiUrl();
            return await _httpClient.GetFromJsonAsync<PlaceDetailsDto>($"{apiUrl}/api/v1/places/{placeId}");
        }
        catch
        {
            return null;
        }
    }

    private string GetApiUrl()
    {
        // Берёт текущий хост из браузера и заменяет порт на 5108
        var baseUrl = _httpClient.BaseAddress?.ToString() ?? "";
        var uri = new Uri(baseUrl);
        return $"{uri.Scheme}://{uri.Host}:5108";
    }
}