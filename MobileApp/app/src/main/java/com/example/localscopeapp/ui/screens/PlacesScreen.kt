package com.example.localscopeapp.ui.screens

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.*
import androidx.fragment.app.Fragment
import com.example.localscopeapp.R
import com.example.localscopeapp.data.LocationHolder
import com.example.localscopeapp.data.api.RetrofitClient
import kotlinx.coroutines.*

class PlacesScreen : Fragment() {

    private lateinit var placesList: LinearLayout
    private lateinit var customCategoryInput: EditText
    private var job: Job? = null

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.fragment_places, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)
        placesList = view.findViewById(R.id.placesList)
        customCategoryInput = view.findViewById(R.id.customCategoryInput)

        view.findViewById<Button>(R.id.restBtn)?.setOnClickListener { loadPlaces("ресторан") }
        view.findViewById<Button>(R.id.shopBtn)?.setOnClickListener { loadPlaces("продукты") }
        view.findViewById<Button>(R.id.pharmBtn)?.setOnClickListener { loadPlaces("аптека") }
        view.findViewById<Button>(R.id.searchBtn)?.setOnClickListener {
            val query = customCategoryInput.text.toString().trim()
            if (query.isNotEmpty()) {
                loadPlaces(query)
            } else {
                Toast.makeText(context, "Введите категорию", Toast.LENGTH_SHORT).show()
            }
        }

        // Загружаем рестораны по умолчанию
        loadPlaces("ресторан")
    }

    private fun loadPlaces(category: String) {
        val lat = LocationHolder.currentLat.value ?: 55.753544
        val lon = LocationHolder.currentLon.value ?: 37.621202

        job?.cancel()
        job = CoroutineScope(Dispatchers.IO).launch {
            try {
                activity?.runOnUiThread {
                    placesList.removeAllViews()
                    placesList.addView(TextView(context).apply {
                        text = "🔍 Ищем: $category ($lat, $lon)..."
                        setPadding(0, 8, 0, 8)
                    })
                }

                val result = RetrofitClient.getApi().getNearbyPlaces(lat, lon, category)

                activity?.runOnUiThread {
                    placesList.removeAllViews()

                    // Заголовок
                    placesList.addView(TextView(context).apply {
                        text = "📋 Найдено: ${result.items.size} мест"
                        textSize = 16f
                        setPadding(0, 8, 0, 16)
                    })

                    if (result.items.isEmpty()) {
                        placesList.addView(TextView(context).apply {
                            text = "Ничего не найдено по запросу \"$category\""
                        })
                    } else {
                        result.items.take(15).forEach { place ->
                            placesList.addView(TextView(context).apply {
                                text = "🏠 ${place.name}\n📍 ${place.address}\n📏 ${place.distanceMeters.toInt()}м\n"
                                setPadding(0, 8, 0, 8)
                                textSize = 14f
                            })
                        }
                    }
                }
            } catch (e: Exception) {
                activity?.runOnUiThread {
                    placesList.removeAllViews()
                    placesList.addView(TextView(context).apply {
                        text = "❌ Ошибка: ${e.message}"
                    })
                }
            }
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        job?.cancel()
    }
}