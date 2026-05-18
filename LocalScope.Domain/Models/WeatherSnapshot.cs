namespace LocalScope.Domain.Models;

/// <summary>
/// Снимок погоды (текущей или прогноз)
/// </summary>
public class WeatherSnapshot
{
    public DateTime Timestamp { get; init; }
    public double TemperatureCelsius { get; init; }
    public double? FeelsLikeCelsius { get; init; }
    public int Humidity { get; init; }
    public double WindSpeedMps { get; init; }
    public double? PrecipitationMm { get; init; }
    public string Condition { get; init; } = "unknown"; // clear, cloudy, rain, snow и т.д.
    public int? CloudCover { get; init; }
    public double? Pressure { get; init; }

    /// <summary>
    /// Оценка комфортности погоды (0-100)
    /// </summary>
    public int GetComfortScore()
    {
        var tempScore = TemperatureCelsius switch
        {
            < -10 => 20,
            < 0 => 40,
            < 10 => 60,
            < 15 => 75,
            < 25 => 100,
            < 30 => 85,
            < 35 => 60,
            _ => 30
        };

        var windScore = WindSpeedMps switch
        {
            < 2 => 100,
            < 5 => 90,
            < 10 => 70,
            < 15 => 40,
            _ => 20
        };

        var precipScore = (PrecipitationMm ?? 0) switch
        {
            < 0.5 => 100,
            < 2 => 80,
            < 5 => 60,
            < 10 => 40,
            _ => 20
        };

        return (int)((tempScore * 0.5 + windScore * 0.3 + precipScore * 0.2));
    }

    /// <summary>
    /// Текстовое описание комфортности
    /// </summary>
    public string GetComfortDescription() => GetComfortScore() switch
    {
        >= 90 => "Отличная погода",
        >= 70 => "Комфортно",
        >= 50 => "Терпимо",
        >= 30 => "Некомфортно",
        _ => "Плохая погода"
    };
}