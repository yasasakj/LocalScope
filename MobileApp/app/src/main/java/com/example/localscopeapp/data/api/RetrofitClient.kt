package com.example.localscopeapp.data.api

import okhttp3.OkHttpClient
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object RetrofitClient {

    private var retrofit: Retrofit? = null
    private var currentBaseUrl: String? = null

    fun getApi(): LocalScopeApi {
        val baseUrl = ApiConfig.getBaseUrl()

        if (retrofit == null || currentBaseUrl != baseUrl) {
            val client = OkHttpClient.Builder()
                .connectTimeout(10, TimeUnit.SECONDS)
                .readTimeout(10, TimeUnit.SECONDS)
                .build()

            retrofit = Retrofit.Builder()
                .baseUrl(baseUrl)
                .client(client)
                .addConverterFactory(GsonConverterFactory.create())
                .build()

            currentBaseUrl = baseUrl
        }

        return retrofit!!.create(LocalScopeApi::class.java)
    }
}