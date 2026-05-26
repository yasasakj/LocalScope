package com.example.localscopeapp.ui.screens

import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.*
import androidx.fragment.app.Fragment
import com.example.localscopeapp.R
import com.example.localscopeapp.data.api.ApiConfig

class SettingsScreen : Fragment() {

    private lateinit var urlInput: EditText
    private lateinit var saveButton: Button
    private lateinit var statusText: TextView
    private lateinit var currentUrlText: TextView

    override fun onCreateView(
        inflater: LayoutInflater,
        container: ViewGroup?,
        savedInstanceState: Bundle?
    ): View? {
        return inflater.inflate(R.layout.fragment_settings, container, false)
    }

    override fun onViewCreated(view: View, savedInstanceState: Bundle?) {
        super.onViewCreated(view, savedInstanceState)

        urlInput = view.findViewById(R.id.urlInput)
        saveButton = view.findViewById(R.id.saveButton)
        statusText = view.findViewById(R.id.statusText)
        currentUrlText = view.findViewById(R.id.currentUrlText)

        // Показать текущий URL
        val currentUrl = ApiConfig.getBaseUrl()
        currentUrlText.text = "Текущий: $currentUrl"
        urlInput.setText(currentUrl)

        saveButton.setOnClickListener {
            saveUrl()
        }
    }

    private fun saveUrl() {
        var url = urlInput.text.toString().trim()

        if (url.isEmpty()) {
            statusText.text = "Введите URL"
            return
        }

        // Добавляем http:// если нет
        if (!url.startsWith("http://") && !url.startsWith("https://")) {
            url = "http://$url"
        }

        // Добавляем / в конец если нет
        if (!url.endsWith("/")) {
            url = "$url/"
        }

        ApiConfig.setBaseUrl(url)
        currentUrlText.text = "Текущий: $url"
        statusText.text = "✅ Сохранено!"

        Toast.makeText(context, "URL сохранён. Перезапустите приложение.", Toast.LENGTH_LONG).show()
    }
}