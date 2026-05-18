namespace LocalScope.Domain.Services;
using LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;


/// <summary>
/// Основной сервис для агрегации данных из разных провайдеров
/// </summary>
public class LocalScopeService
{
    private readonly IGeocodingProvider _geocodingProvider;
    private readonly IPlacesProvider _placesProvider;
    private readonly IWeatherProvider _weatherProvider;

    public LocalScopeService(
        IGeocodingProvider geocodingProvider,
        IPlacesProvider placesProvider,
        IWeatherProvider weatherProvider)
    {
        _geocodingProvider = geocodingProvider;
        _placesProvider = placesProvider;
        _weatherProvider = weatherProvider;
    }

    /// <summary>
    /// Получить полную сводку: погода + места рядом
    /// </summary>
    public async Task<LocationSummary> GetLocationSummaryAsync(
        GeoPoint location,
        int radiusMeters = 1000,
        CancellationToken cancellationToken = default)
    {
        var weatherTask = _weatherProvider.GetCurrentWeatherAsync(location, cancellationToken);
        var placesTask = _placesProvider.SearchNearbyAsync(location, radiusMeters: radiusMeters, cancellationToken: cancellationToken);

        await Task.WhenAll(weatherTask, placesTask);

        var weather = await weatherTask;
        var places = await placesTask;

        // Сортируем по скорингу
        var sortedPlaces = places
            .OrderByDescending(p => p.CalculateScore(location, radiusMeters))
            .ToList();

        return new LocationSummary(location, weather, sortedPlaces);
    }

    /// <summary>
    /// Поиск по адресу → сводка
    /// </summary>
    public async Task<LocationSummary?> GetSummaryByAddressAsync(
        string address,
        int radiusMeters = 1000,
        CancellationToken cancellationToken = default)
    {
        var geocodeResult = await _geocodingProvider.GeocodeAsync(address, cancellationToken);
        if (geocodeResult == null)
            return null;

        return await GetLocationSummaryAsync(geocodeResult.Location, radiusMeters, cancellationToken);
    }
}

public record LocationSummary(
    GeoPoint Location,
    WeatherSnapshot? Weather,
    IReadOnlyList<Place> NearbyPlaces
);
