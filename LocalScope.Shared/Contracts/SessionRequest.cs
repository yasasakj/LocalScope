namespace LocalScope.Shared.Contracts;

public record SessionRequest
{
    public string DeviceId { get; init; } = string.Empty;
    public string? PreferredCity { get; init; }
    public string Units { get; init; } = "metric";
    public int DefaultRadiusMeters { get; init; } = 1200;
}

public record SessionResponse
{
    public string SessionToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}
