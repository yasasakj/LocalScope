package com.example.localscopeapp.data

import androidx.lifecycle.MutableLiveData

object LocationHolder {
    val currentLat = MutableLiveData(55.753544)
    val currentLon = MutableLiveData(37.621202)

    fun setLocation(lat: Double, lon: Double) {
        currentLat.postValue(lat)
        currentLon.postValue(lon)
    }
}