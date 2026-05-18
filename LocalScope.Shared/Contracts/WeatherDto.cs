namespace LocalScope.Shared.Contracts;

public record WeatherRequest
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int Days { get; init; } = 3;
}

public record WeatherResponse
{
    public WeatherSnapshotDto Now { get; init; } = null!;
    public List<WeatherSnapshotDto> Forecast { get; init; } = new();
}

public record WeatherSnapshotDto
{
    public DateTime Timestamp { get; init; }
    public double TemperatureCelsius { get; init; }
    public double? FeelsLikeCelsius { get; init; }
    public int Humidity { get; init; }
    public double WindSpeedMps { get; init; }
    public double? PrecipitationMm { get; init; }
    public string Condition { get; init; } = string.Empty;
    public int? CloudCover { get; init; }
    public int? ComfortScore { get; init; } // Сделали nullable, так как в UI идет проверка .HasValue
    public string ComfortDescription { get; init; } = string.Empty;
}

/// <summary>
/// Единый DTO для фронтенда Blazor, объединяющий текущую погоду и прогноз
/// </summary>
public record WeatherDto : WeatherSnapshotDto
{
    /// <summary>
    /// Прогноз погоды на несколько дней вперед
    /// </summary>
    public List<WeatherSnapshotDto> Forecast { get; init; } = new();
}