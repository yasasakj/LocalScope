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
    public int ComfortScore { get; init; }
    public string ComfortDescription { get; init; } = string.Empty;
}
