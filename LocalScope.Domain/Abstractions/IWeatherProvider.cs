namespace LocalScope.Domain.Abstractions;
using LocalScope.Domain.Models;


/// <summary>
/// Провайдер информации о погоде
/// </summary>
public interface IWeatherProvider
{
    /// <summary>
    /// Получить текущую погоду
    /// </summary>
    Task<WeatherSnapshot?> GetCurrentWeatherAsync(GeoPoint location, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получить прогноз погоды на несколько дней
    /// </summary>
    Task<IReadOnlyList<WeatherSnapshot>> GetForecastAsync(
        GeoPoint location,
        int days = 3,
        CancellationToken cancellationToken = default);
}
