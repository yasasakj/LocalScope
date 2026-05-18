using LocalScope.Domain.Models;

namespace LocalScope.Domain.Abstractions;

/// <summary>
/// Провайдер геокодирования (адрес → координаты)
/// </summary>
public interface IGeocodingProvider
{
    /// <summary>
    /// Получить координаты по адресу или строке поиска
    /// </summary>
    Task<GeocodingResult?> GeocodeAsync(string query, CancellationToken cancellationToken = default);
}

public record GeocodingResult(
    GeoPoint Location,
    string FormattedAddress,
    string Precision // exact, street, district, city
);