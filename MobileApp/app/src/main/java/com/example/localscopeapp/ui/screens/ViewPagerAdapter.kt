package com.example.localscopeapp.ui.screens

import androidx.fragment.app.Fragment
import androidx.fragment.app.FragmentActivity
import androidx.viewpager2.adapter.FragmentStateAdapter

class ViewPagerAdapter(activity: FragmentActivity) : FragmentStateAdapter(activity) {

    override fun getItemCount(): Int = 4  // Было 3, стало 4

    override fun createFragment(position: Int): Fragment {
        return when (position) {
            0 -> HomeScreen()
            1 -> WeatherScreen()
            2 -> PlacesScreen()
            3 -> SettingsScreen()  // Новая вкладка
            else -> HomeScreen()
        }
    }
}