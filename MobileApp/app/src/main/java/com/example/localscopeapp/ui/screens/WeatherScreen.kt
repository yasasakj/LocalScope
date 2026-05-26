package com.example.localscopeapp.ui.screens

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.*
import androidx.fragment.app.Fragment
import androidx.lifecycle.Observer
import com.example.localscopeapp.R
import com.example.localscopeapp.data.LocationHolder
import com.example.localscopeapp.data.api.RetrofitClient
import kotlinx.coroutines.*

class WeatherScreen : Fragment() {

    private lateinit var weatherText: TextView
    private var job: Job? = null

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.fragment_weather, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        weatherText = view.findViewById(R.id.weatherText)

        // Наблюдаем за изменением локации
        LocationHolder.currentLat.observe(viewLifecycleOwner) {
            loadWeather()
        }

        loadWeather()
    }

    private fun loadWeather() {
        val lat = LocationHolder.currentLat.value ?: 55.753544
        val lon = LocationHolder.currentLon.value ?: 37.621202

        job?.cancel()
        job = CoroutineScope(Dispatchers.IO).launch {
            try {
                activity?.runOnUiThread {
                    weatherText.text = "Загрузка погоды..."
                }
                val response = RetrofitClient.getApi().getWeather(lat, lon)
                val weather = response.now  // ← Берём now из ответа
                val forecast = response.forecast

                activity?.runOnUiThread {
                    val forecastText = forecast.take(3).joinToString("\n") { f ->
                        "📅 ${f.timestamp.take(10)}: ${f.temperatureCelsius}°C, ${f.condition}"
                    }

                    weatherText.text = """
                    🌤️ ${weather.condition}
                    🌡️ Температура: ${weather.temperatureCelsius}°C
                    💧 Ощущается: ${weather.feelsLikeCelsius ?: "—"}°C
                    💨 Ветер: ${weather.windSpeedMps} м/с
                    💧 Влажность: ${weather.humidity}%
                    ☁️ Облачность: ${weather.cloudCover ?: "—"}%
                    
                    📋 Прогноз:
                    $forecastText
                """.trimIndent()
                }
            } catch (e: Exception) {
                activity?.runOnUiThread {
                    weatherText.text = "Ошибка загрузки погоды: ${e.message}"
                }
            }
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        job?.cancel()
    }
}