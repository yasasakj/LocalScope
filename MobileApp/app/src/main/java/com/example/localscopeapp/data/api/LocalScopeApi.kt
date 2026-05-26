package com.example.localscopeapp.data.api

import com.example.localscopeapp.data.models.GeocodeResponse
import com.example.localscopeapp.data.models.PlacesNearbyResponse
import com.example.localscopeapp.data.models.WeatherResponse
import retrofit2.http.GET
import retrofit2.http.Query

interface LocalScopeApi {

    @GET("api/v1/geocode")
    suspend fun geocode(
        @Query("query") query: String
    ): GeocodeResponse

    @GET("api/v1/weather")
    suspend fun getWeather(
        @Query("lat") lat: Double,
        @Query("lon") lon: Double,
        @Query("days") days: Int = 3
    ): WeatherResponse

    @GET("api/v1/places/nearby")
    suspend fun getNearbyPlaces(
        @Query("lat") lat: Double,
        @Query("lon") lon: Double,
        @Query("category") category: String? = null,
        @Query("radius") radius: Int = 1500,
        @Query("limit") limit: Int = 10
    ): PlacesNearbyResponse
}