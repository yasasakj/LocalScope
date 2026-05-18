using System.Net.Http.Json;
using LocalScope.Shared.Contracts;

namespace LocalScope.Web.Services.ApiClient;

/// <summary>
/// Typed HTTP client для вызовов LocalScope API с фронтенда
/// </summary>
public class LocalScopeApiClient
{
    private readonly HttpClient _httpClient;

    public LocalScopeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SessionResponse?> CreateSessionAsync(SessionRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/v1/sessions", request);
            if (!response.IsSuccessStatusCode) return null;
            
            return await response.Content.ReadFromJsonAsync<SessionResponse>();
        }
        catch
        {
            return null; // Возвращаем null, чтобы UI обработал ошибку подключения
        }
    }

    public async Task<GeocodeResponse?> GeocodeAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return null;

        try
        {
            // Убрали лишний слэш в начале URL, чтобы относительные пути HttpClient работали корректно
            return await _httpClient.GetFromJsonAsync<GeocodeResponse>($"api/v1/geocode?query={Uri.EscapeDataString(query)}");
        }
        catch (HttpRequestException)
        {
            throw new Exception("Не удалось связаться с сервером геокодирования. Проверьте подключение к сети.");
        }
    }

    public async Task<WeatherResponse?> GetWeatherAsync(double lat, double lon, int days = 3)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<WeatherResponse>($"api/v1/weather?lat={lat}&lon={lon}&days={days}");
        }
        catch (HttpRequestException)
        {
            throw new Exception("Не удалось загрузить данные о погоде от сервера LocalScope.");
        }
    }

    public async Task<PlacesNearbyResponse?> SearchPlacesAsync(double lat, double lon, string? category = null, int radius = 1000, int limit = 20)
    {
        var url = $"api/v1/places/nearby?lat={lat}&lon={lon}&radius={radius}&limit={limit}";
        if (!string.IsNullOrEmpty(category))
        {
            url += $"&category={Uri.EscapeDataString(category)}";
        }

        try
        {
            return await _httpClient.GetFromJsonAsync<PlacesNearbyResponse>(url);
        }
        catch (HttpRequestException)
        {
            throw new Exception("Ошибка при получении списка заведений неподалеку.");
        }
    }

    public async Task<PlaceDetailsDto?> GetPlaceDetailsAsync(string placeId)
    {
        if (string.IsNullOrWhiteSpace(placeId)) return null;

        try
        {
            return await _httpClient.GetFromJsonAsync<PlaceDetailsDto>($"api/v1/places/{placeId}");
        }
        catch
        {
            return null;
        }
    }
}
