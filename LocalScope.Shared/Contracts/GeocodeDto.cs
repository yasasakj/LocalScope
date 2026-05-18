namespace LocalScope.Shared.Contracts;

public record GeocodeRequest
{
    public string Query { get; init; } = string.Empty;
}

public record GeocodeResponse
{
    public string FormattedAddress { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string Precision { get; init; } = string.Empty;
}
