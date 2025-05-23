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
} // namespace TimeUtils

namespace TimeConstants {
using namespace std::chrono_literals;

constexpr auto WIFI_CONNECTION_TIMEOUT = 10s;
constexpr auto WIFI_RETRY_DELAY = 1s;
constexpr auto WIFI_AP_MODE_TIMEOUT = 5min;
constexpr auto WIFI_STABILIZATION_DELAY = 1s;
constexpr auto WIFI_RESTART_DELAY = 500ms;

constexpr auto MQTT_RETRY_DELAY = 5s;
constexpr auto ERROR_STATE_DELAY = 5s;
constexpr auto STATE_CHECK_INTERVAL = 100ms;

constexpr auto BUTTON_LONG_PRESS = 5s;
constexpr auto BUTTON_SHORT_PRESS = 1s;
constexpr auto BUTTON_LED_BLINK = 50ms;

constexpr auto MQTT_KEEP_ALIVE = 60s;
constexpr auto MQTT_SOCKET_TIMEOUT_DURATION = 30s;
} // namespace TimeConstants

#endif