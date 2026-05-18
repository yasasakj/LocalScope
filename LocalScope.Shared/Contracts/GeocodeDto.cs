namespace LocalScope.Shared.Contracts;

/// <summary>
/// Запрос на геокодирование от клиента к API
/// </summary>
public record GeocodeRequest
{
    public string Query { get; init; } = string.Empty;
}

/// <summary>
/// Объект передачи данных (DTO) для результатов геокодирования
/// </summary>
public record GeocodeDto
{
    public string FormattedAddress { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string Precision { get; init; } = string.Empty;
}

/// <summary>
/// Алиас (дубликат) для обратной совместимости, если где-то в коде временно используется старое имя
/// </summary>
public record GeocodeResponse : GeocodeDto { }