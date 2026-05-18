using System.Net.Http.Json;
using LocalScope.Shared.Contracts;

namespace LocalScope.Web.Services.ApiClient;

/// <summary>
/// Typed HTTP client для вызовов LocalScope API
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
        var response = await _httpClient.PostAsJsonAsync("/api/v1/sessions", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SessionResponse>();
    }

    public async Task<GeocodeResponse?> GeocodeAsync(string query)
    {
        return await _httpClient.GetFromJsonAsync<GeocodeResponse>($"/api/v1/geocode?query={Uri.EscapeDataString(query)}");
    }

    public async Task<WeatherResponse?> GetWeatherAsync(double lat, double lon, int days = 3)
    {
        return await _httpClient.GetFromJsonAsync<WeatherResponse>($"/api/v1/weather?lat={lat}&lon={lon}&days={days}");
    }

    public async Task<PlacesNearbyResponse?> SearchPlacesAsync(double lat, double lon, string? category = null, int radius = 1000, int limit = 20)
    {
        var url = $"/api/v1/places/nearby?lat={lat}&lon={lon}&radius={radius}&limit={limit}";
        if (!string.IsNullOrEmpty(category))
            url += $"&category={Uri.EscapeDataString(category)}";

        return await _httpClient.GetFromJsonAsync<PlacesNearbyResponse>(url);
    }

    public async Task<PlaceDetailsDto?> GetPlaceDetailsAsync(string placeId)
    {
        return await _httpClient.GetFromJsonAsync<PlaceDetailsDto>($"/api/v1/places/{placeId}");
    }
}
