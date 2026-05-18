namespace LocalScope.Shared.Contracts;

public record FavoritePlaceRequest
{
    public string PlaceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? Category { get; init; }
}

public record FavoritePlaceResponse
{
    public string Id { get; init; } = string.Empty;
    public string PlaceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? Category { get; init; }
    public DateTime SavedAt { get; init; }
}

public record FavoritePlacesListResponse
{
    public List<FavoritePlaceResponse> Favorites { get; init; } = new();
}