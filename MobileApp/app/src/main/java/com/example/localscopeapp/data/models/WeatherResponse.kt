package com.example.localscopeapp.data.models

data class WeatherResponse(
    val now: WeatherNow,
    val forecast: List<WeatherForecast>
)

data class WeatherNow(
    val timestamp: String,
    val temperatureCelsius: Double,
    val feelsLikeCelsius: Double?,
    val humidity: Int,
    val windSpeedMps: Double,
    val precipitationMm: Double?,
    val condition: String,
    val cloudCover: Int?,
    val comfortScore: Int?,
    val comfortDescription: String?
)

data class WeatherForecast(
    val timestamp: String,
    val temperatureCelsius: Double,
    val feelsLikeCelsius: Double?,
    val humidity: Int,
    val windSpeedMps: Double,
    val precipitationMm: Double?,
    val condition: String,
    val cloudCover: Int?,
    val comfortScore: Int?,
    val comfortDescription: String?
)