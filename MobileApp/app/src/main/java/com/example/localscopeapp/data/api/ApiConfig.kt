package com.example.localscopeapp.data.api

import android.content.Context
import android.content.SharedPreferences

object ApiConfig {
    private const val PREFS_NAME = "api_config"
    private const val KEY_BASE_URL = "base_url"
    private const val DEFAULT_URL = "http://192.168.31.201:5108/"

    private var prefs: SharedPreferences? = null

    fun init(context: Context) {
        prefs = context.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)
    }

    fun getBaseUrl(): String {
        return prefs?.getString(KEY_BASE_URL, DEFAULT_URL) ?: DEFAULT_URL
    }

    fun setBaseUrl(url: String) {
        prefs?.edit()?.putString(KEY_BASE_URL, url)?.apply()
    }
}