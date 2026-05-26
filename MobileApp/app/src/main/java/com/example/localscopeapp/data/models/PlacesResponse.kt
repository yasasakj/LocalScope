package com.example.localscopeapp.data.models

data class GeocodeResponse(
    val latitude: Double,
    val longitude: Double,
    val formattedAddress: String
)

data class PlacesNearbyResponse(
    val center: LocationDto,
    val category: String?,
    val radiusMeters: Int,
    val items: List<PlaceDto>
)

data class PlaceDto(
    val id: String,
    val name: String,
    val address: String,
    val location: LocationDto,
    val distanceMeters: Double,
    val isOpenNow: Boolean?,
    val rating: Double?,
    val categories: List<String>?
)

data class LocationDto(
    val latitude: Double,
    val longitude: Double
)