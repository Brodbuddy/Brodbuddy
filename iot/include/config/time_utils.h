#ifndef TIME_UTILS_H
#define TIME_UTILS_H

#include <chrono>
#include <Arduino.h>

namespace TimeUtils {
    using namespace std::chrono;
    using namespace std::chrono_literals;

    template <typename Rep, typename Period> inline void delay_for(const duration<Rep, Period>& d) {
        auto ms = duration_cast<milliseconds>(d);
        delay(ms.count());
    }

    template <typename Rep, typename Period> inline void enable_sleep_timer(const duration<Rep, Period>& d) {
        auto us = duration_cast<microseconds>(d);
        esp_sleep_enable_timer_wakeup(us.count());
    }

    template <typename Rep, typename Period> inline unsigned long to_ms(const duration<Rep, Period>& d) {
        return duration_cast<milliseconds>(d).count();
    }

    template <typename Rep, typename Period> inline uint64_t to_us(const duration<Rep, Period>& d) {
        return duration_cast<microseconds>(d).count();
    }

    template <typename Rep, typename Period> inline int to_seconds(const duration<Rep, Period>& d) {
        return duration_cast<seconds>(d).count();
    }

    template <typename Rep, typename Period> inline int to_minutes(const duration<Rep, Period>& d) {
        return duration_cast<minutes>(d).count();
    }
}

namespace TimeConstants {
    using namespace std::chrono_literals;

    // WiFI
    constexpr auto WIFI_CONNECTION_TIMEOUT = 10s;
    constexpr auto WIFI_RETRY_DELAY = 1s;
    constexpr auto WIFI_AP_MODE_TIMEOUT = 5min;
    constexpr auto WIFI_STABILIZATION_DELAY = 1s;
    constexpr auto WIFI_RESTART_DELAY = 500ms;
    constexpr auto WIFI_CHECK_INTERVAL = 10s;

    constexpr auto MQTT_RETRY_DELAY = 5s;
    constexpr auto ERROR_STATE_DELAY = 5s;
    constexpr auto STATE_CHECK_INTERVAL = 100ms;

    constexpr auto BUTTON_LONG_PRESS = 5s;
    constexpr auto BUTTON_SHORT_PRESS = 1s;
    constexpr auto BUTTON_LED_BLINK = 50ms;


    // LED blink intervaller
    constexpr auto LED_BLINK_FAST = 100ms;
    constexpr auto LED_BLINK_NORMAL = 500ms;
    constexpr auto LED_BLINK_SLOW = 1s;

    constexpr auto GRAPH_WINDOW = 10min;
    
    // MQTT
    constexpr auto MQTT_KEEP_ALIVE = 60s;
    constexpr auto MQTT_SOCKET_TIMEOUT_DURATION = 30s;

    // Sensor
    constexpr auto VL53L0X_TIMEOUT = 500ms;
    constexpr auto VL53L0X_TIMING_BUDGET = 200ms;
    constexpr auto BME280_STABILIZATION_DELAY = 2s;
    constexpr auto VL53L0X_STABILIZATION_DELAY = 1s;
    constexpr auto I2C_INIT_DELAY = 50ms;
    constexpr auto XSHUT_RESET_DELAY = 100ms;
    constexpr auto SENSOR_SAMPLE_INTERVAL = 1s;

    // E-paper display
    constexpr auto EPAPER_RESET_HIGH_DELAY = 200ms;
    constexpr auto EPAPER_RESET_LOW_DELAY = 500ms;
    constexpr auto EPAPER_BUSY_POLL_DELAY = 10ms;
    constexpr auto EPAPER_BUSY_TIMEOUT = 5s;

    // NTP Time sync
    constexpr auto NTP_SYNC_INTERVAL = 24h;
    constexpr auto NTP_SYNC_RETRY_DELAY = 500ms;
    constexpr auto LONG_SLEEP_THRESHOLD = 1h;
    constexpr time_t MIN_VALID_EPOCH = 1609459200;   // 1 januar 2021
    constexpr time_t FALLBACK_EPOCH = 1704067200UL;  // 1 januar 2024
}

#endif