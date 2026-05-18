namespace LocalScope.Shared.Contracts;

public record PlacesNearbyRequest
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? Category { get; init; }
    public int RadiusMeters { get; init; } = 1000;
    public bool? OpenNow { get; init; }
    public int Limit { get; init; } = 20;
}

public record PlacesNearbyResponse
{
    public LocationDto Center { get; init; } = null!;
    public string? Category { get; init; }
    public int RadiusMeters { get; init; }
    public List<PlaceSummaryDto> Items { get; init; } = new();
}

public record PlaceSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public LocationDto Location { get; init; } = null!;
    public double DistanceMeters { get; init; }
    public bool? IsOpenNow { get; init; }
    public double? Rating { get; init; }
    public string[]? Categories { get; init; }
    public double? Score { get; init; } // Сделали nullable, так как на сервере мы проверяли .HasValue
    public string Source { get; init; } = string.Empty;
}

public record PlaceDetailsDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public LocationDto Location { get; init; } = null!;
    public string[]? Categories { get; init; }
    public bool? IsOpenNow { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public double? Rating { get; init; }
    public string? Description { get; init; }
    public string Source { get; init; } = string.Empty;
}

public record LocationDto
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}

/// <summary>
/// Алиас-заглушка для обратной совместимости с Blazor-компонентами
/// </summary>
public record PlacesDto : PlaceSummaryDto { }
