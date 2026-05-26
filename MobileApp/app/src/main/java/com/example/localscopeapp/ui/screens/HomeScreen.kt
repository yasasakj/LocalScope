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

class HomeScreen : Fragment() {

    private lateinit var addressInput: EditText
    private lateinit var searchButton: Button
    private lateinit var resultText: TextView
    private var job: Job? = null

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.fragment_home, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        addressInput = view.findViewById(R.id.addressInput)
        searchButton = view.findViewById(R.id.searchButton)
        resultText = view.findViewById(R.id.resultText)

        searchButton.setOnClickListener {
            searchAddress()
        }
    }

    private fun searchAddress() {
        val query = addressInput.text.toString()
        if (query.isEmpty()) {
            Toast.makeText(context, "Введите адрес", Toast.LENGTH_SHORT).show()
            return
        }

        job?.cancel()
        job = CoroutineScope(Dispatchers.IO).launch {
            try {
                activity?.runOnUiThread {
                    resultText.text = "Загрузка..."
                }
                val result = RetrofitClient.getApi().geocode(query)

                // Сохраняем локацию
                LocationHolder.setLocation(result.latitude, result.longitude)

                activity?.runOnUiThread {
                    resultText.text = "📍 ${result.formattedAddress}\n📐 ${result.latitude}, ${result.longitude}\n✅ Локация обновлена!"
                }
            } catch (e: Exception) {
                activity?.runOnUiThread {
                    resultText.text = "Ошибка: ${e.message}"
                }
            }
        }
    }

    override fun onDestroyView() {
        super.onDestroyView()
        job?.cancel()
    }
}