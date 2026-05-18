namespace LocalScope.Domain.Models;

/// <summary>
/// Место/организация (кафе, аптека и т.д.)
/// </summary>
public class Place
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public GeoPoint Location { get; init; } = null!;
    public string[]? Categories { get; init; }
    public bool? IsOpenNow { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public double? Rating { get; init; }
    public string? Description { get; init; }
    public string Source { get; init; } = "unknown"; // yandex, 2gis и т.д.

    /// <summary>
    /// Расстояние от заданной точки в метрах
    /// </summary>
    public double GetDistanceFrom(GeoPoint point) => Location.DistanceTo(point);

    /// <summary>
    /// Простой скоринг места на основе рейтинга и расстояния
    /// </summary>
    public double CalculateScore(GeoPoint fromPoint, int maxDistanceMeters = 2000)
    {
        var distance = GetDistanceFrom(fromPoint);
        var distanceScore = Math.Max(0, 1 - (distance / maxDistanceMeters));
        var ratingScore = Rating.HasValue ? Rating.Value / 5.0 : 0.5;
        var openScore = IsOpenNow.HasValue ? (IsOpenNow.Value ? 1.0 : 0.3) : 0.7;

        return (distanceScore * 0.4 + ratingScore * 0.4 + openScore * 0.2) * 100;
    }
}