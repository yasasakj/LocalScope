using LocalScope.Domain.Models;

namespace LocalScope.Api.Tests;

public class GeoPointTests
{
    [Fact]
    public void GeoPoint_ValidCoordinates_CreatesSuccessfully()
    {
        // Arrange & Act
        var point = new GeoPoint(55.7558, 37.6173); // Москва

        // Assert
        Assert.Equal(55.7558, point.Latitude);
        Assert.Equal(37.6173, point.Longitude);
    }

    [Fact]
    public void GeoPoint_InvalidLatitude_ThrowsException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(91, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(-91, 0));
    }

    [Fact]
    public void GeoPoint_InvalidLongitude_ThrowsException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(0, 181));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GeoPoint(0, -181));
    }

    [Fact]
    public void DistanceTo_CalculatesCorrectDistance()
    {
        // Arrange
        var moscow = new GeoPoint(55.7558, 37.6173);
        var spb = new GeoPoint(59.9343, 30.3351);

        // Act
        var distance = moscow.DistanceTo(spb);

        // Assert - примерно 635 км между Москвой и СПб
        Assert.InRange(distance, 630000, 640000);
    }
}

public class WeatherSnapshotTests
{
    [Fact]
    public void GetComfortScore_PerfectWeather_Returns100()
    {
        // Arrange
        var weather = new WeatherSnapshot
        {
            Timestamp = DateTime.UtcNow,
            TemperatureCelsius = 22,
            Humidity = 50,
            WindSpeedMps = 1,
            PrecipitationMm = 0,
            Condition = "clear"
        };

        // Act
        var score = weather.GetComfortScore();

        // Assert
        Assert.InRange(score, 95, 100);
    }

    [Fact]
    public void GetComfortScore_BadWeather_ReturnsLowScore()
    {
        // Arrange
        var weather = new WeatherSnapshot
        {
            Timestamp = DateTime.UtcNow,
            TemperatureCelsius = -15,
            Humidity = 90,
            WindSpeedMps = 20,
            PrecipitationMm = 15,
            Condition = "snow"
        };

        // Act
        var score = weather.GetComfortScore();

        // Assert
        Assert.InRange(score, 0, 30);
    }

    [Fact]
    public void GetComfortDescription_ReturnsCorrectDescription()
    {
        // Arrange
        var excellentWeather = new WeatherSnapshot
        {
            Timestamp = DateTime.UtcNow,
            TemperatureCelsius = 23,
            WindSpeedMps = 1,
            PrecipitationMm = 0,
            Humidity = 50,
            Condition = "clear"
        };

        // Act
        var description = excellentWeather.GetComfortDescription();

        // Assert
        Assert.Equal("Отличная погода", description);
    }
}

public class PlaceTests
{
    [Fact]
    public void GetDistanceFrom_CalculatesCorrectDistance()
    {
        // Arrange
        var center = new GeoPoint(55.7558, 37.6173); // Москва центр
        var place = new Place
        {
            Id = "1",
            Name = "Test Place",
            Address = "Test Address",
            Location = new GeoPoint(55.7600, 37.6200),
            Source = "test"
        };

        // Act
        var distance = place.GetDistanceFrom(center);

        // Assert
        Assert.InRange(distance, 0, 1000); // Должно быть меньше 1 км
    }

    [Fact]
    public void CalculateScore_OpenPlaceNearby_ReturnsHighScore()
    {
        // Arrange
        var center = new GeoPoint(55.7558, 37.6173);
        var place = new Place
        {
            Id = "1",
            Name = "Nearby Cafe",
            Address = "Test",
            Location = new GeoPoint(55.7560, 37.6175), // Очень близко
            Rating = 4.5,
            IsOpenNow = true,
            Source = "test"
        };

        // Act
        var score = place.CalculateScore(center, 1000);

        // Assert
        Assert.InRange(score, 85, 100);
    }

    [Fact]
    public void CalculateScore_ClosedPlaceFarAway_ReturnsLowScore()
    {
        // Arrange
        var center = new GeoPoint(55.7558, 37.6173);
        var place = new Place
        {
            Id = "1",
            Name = "Far Cafe",
            Address = "Test",
            Location = new GeoPoint(55.8000, 37.7000), // Далеко
            Rating = 2.0,
            IsOpenNow = false,
            Source = "test"
        };

        // Act
        var score = place.CalculateScore(center, 1000);

        // Assert
        Assert.InRange(score, 0, 40);
    }
}