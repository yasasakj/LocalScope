using LocalScope.Domain.Models;

namespace LocalScope.Domain.Abstractions;

/// <summary>
/// Провайдер поиска мест/организаций
/// </summary>
public interface IPlacesProvider
{
    /// <summary>
    /// Поиск мест рядом с точкой
    /// </summary>
    Task<IReadOnlyList<Place>> SearchNearbyAsync(
        GeoPoint center,
        string? category = null,
        int radiusMeters = 1000,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить детальную информацию о месте
    /// </summary>
    Task<Place?> GetPlaceDetailsAsync(string placeId, CancellationToken cancellationToken = default);
}