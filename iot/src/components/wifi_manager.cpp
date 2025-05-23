#include "components/wifi_manager.h"
#include <Arduino.h>
#include <utils/constants.h>
#include <utils/time_utils.h>
#include <utils/logger.h>

static const char* TAG = "WiFiManager";

#define BLINK_INTERVAL LED_BLINK_NORMAL

TaskHandle_t blinkTaskHandle = NULL;

WifiManager::WifiManager()
    : currentStatus(WIFI_DISCONNECTED), connectStartTime(0), previousMillis(0), ledState(LOW), lastWiFiCheck(0),
      apModeStartTime(0), apModeTimeoutEnabled(true), apModeTimeoutOccurred(false) {}

void WifiManager::begin() {
    pinMode(Pins::LED, OUTPUT);

    currentStatus = WIFI_CONNECTING;
    ledState = LOW;
    digitalWrite(Pins::LED, ledState);
    connectStartTime = millis();

    createBlinkTask();

    LOG_I(TAG, "Attempting to connect with saved credentials");

    preferences.begin(WiFiConstants::PREFERENCES_NAMESPACE, false);
    String ssid = preferences.getString(WiFiConstants::PREF_KEY_SSID, "");
    String password = preferences.getString(WiFiConstants::PREF_KEY_PASSWORD, "");
    preferences.end();

    if (ssid.length() > 0) {
        LOG_I(TAG, "Found saved SSID: %s", ssid.c_str());
        WiFi.begin(ssid.c_str(), password.c_str());
    } else {
        WiFi.begin();
    }

    int timeout =
        TimeUtils::to_ms(TimeConstants::WIFI_CONNECTION_TIMEOUT) / TimeUtils::to_ms(TimeConstants::WIFI_RETRY_DELAY);
    while (WiFi.status() != WL_CONNECTED && timeout > 0) {
        TimeUtils::delay_for(TimeConstants::WIFI_RETRY_DELAY);
        LOG_D(TAG, "Connecting...");
        timeout--;
    }

    if (WiFi.status() == WL_CONNECTED) {
        LOG_I(TAG, "Connected to WiFi!");
        LOG_I(TAG, "IP address: %s", WiFi.localIP().toString().c_str());

        WiFi.setHostname(HOSTNAME);

        currentStatus = WIFI_CONNECTED;
        digitalWrite(Pins::LED, HIGH);
    } else {
        LOG_W(TAG, "Failed to connect with saved credentials, starting custom portal");
        captivePortalManager.setSaveCredentialsCallback(
            [this](const String& ssid, const String& password) { this->saveWiFiCredentials(ssid, password); });
        captivePortalManager.setStatusCallback([this](int status) { this->currentStatus = (WiFiStatus)status; });
        captivePortalManager.setBlinkTaskCallback([this]() { this->createBlinkTask(); });
        captivePortalManager.startCustomPortal();
        apModeStartTime = millis();
    }
}

void WifiManager::loop() {
    captivePortalManager.loop();

    if (captivePortalManager.isRunning()) {
        if (apModeTimeoutEnabled &&
            (millis() - apModeStartTime > TimeUtils::to_ms(TimeConstants::WIFI_AP_MODE_TIMEOUT))) {
            LOG_W(TAG, "AP mode timeout occurred");
            apModeTimeoutOccurred = true;
        }
    }

    wl_status_t wifiStatus = WiFi.status();
    if (wifiStatus == WL_CONNECTED && currentStatus != WIFI_CONNECTED) {
        currentStatus = WIFI_CONNECTED;
    } else if (wifiStatus != WL_CONNECTED && currentStatus == WIFI_CONNECTED) {
        currentStatus = WIFI_DISCONNECTED;
    }

    if (currentStatus == WIFI_CONNECTED) {
        digitalWrite(Pins::LED, HIGH);
    } else if (currentStatus == WIFI_CONNECTING && !captivePortalManager.isRunning()) {
        unsigned long currentMillis = millis();
        if (currentMillis - previousMillis >= BLINK_INTERVAL) {
            previousMillis = currentMillis;
            ledState = !ledState;
            digitalWrite(Pins::LED, ledState);
        }
    }

    checkWiFiStatus();
}

void WifiManager::createBlinkTask() {
    if (blinkTaskHandle != NULL) {
        vTaskDelete(blinkTaskHandle);
        blinkTaskHandle = NULL;
    }

    xTaskCreate(
        [](void* parameter) {
            bool localLedState = false;
            while (true) {
                localLedState = !localLedState;
                digitalWrite(Pins::LED, localLedState ? HIGH : LOW);
                vTaskDelay(BLINK_INTERVAL / portTICK_PERIOD_MS);
            }
        },
        WiFiConstants::BLINK_TASK_NAME, WiFiConstants::BLINK_TASK_STACK_SIZE, NULL, WiFiConstants::BLINK_TASK_PRIORITY,
        &blinkTaskHandle);
}

void WifiManager::checkWiFiStatus() {
    if (millis() - lastWiFiCheck > WIFI_CHECK_INTERVAL) {
        lastWiFiCheck = millis();

        if (currentStatus == WIFI_CONNECTED) {
            if (WiFi.status() != WL_CONNECTED) {
                LOG_W(TAG, "WiFi connection lost");
                currentStatus = WIFI_DISCONNECTED;
            }
        } else if (currentStatus == WIFI_DISCONNECTED) {
            // Update status based on actual WiFi state
            if (WiFi.status() == WL_CONNECTED) {
                currentStatus = WIFI_CONNECTED;
                LOG_I(TAG, "WiFi reconnected");
            }
        }
    }
}

void WifiManager::saveWiFiCredentials(const String& ssid, const String& password) {
    preferences.begin(WiFiConstants::PREFERENCES_NAMESPACE, false);
    preferences.putString(WiFiConstants::PREF_KEY_SSID, ssid);
    preferences.putString(WiFiConstants::PREF_KEY_PASSWORD, password);
    preferences.end();
}

void WifiManager::resetSettings() {
    preferences.begin(WiFiConstants::PREFERENCES_NAMESPACE, false);
    preferences.clear();
    preferences.end();

    WiFi.disconnect(true);
}

void WifiManager::enableAPModeTimeout() {
    apModeTimeoutEnabled = true;
    apModeStartTime = millis();
}

void WifiManager::disableAPModeTimeout() {
    apModeTimeoutEnabled = false;
}

WiFiStatus WifiManager::getStatus() const {
    return currentStatus;
}

bool WifiManager::hasError() const {
    return apModeTimeoutOccurred;
}

void WifiManager::startCaptivePortal() {
    LOG_I(TAG, "Starting captive portal on demand");
    captivePortalManager.setSaveCredentialsCallback(
        [this](const String& ssid, const String& password) { this->saveWiFiCredentials(ssid, password); });
    captivePortalManager.setStatusCallback([this](int status) { this->currentStatus = (WiFiStatus)status; });
    captivePortalManager.setBlinkTaskCallback([this]() { this->createBlinkTask(); });
    captivePortalManager.startCustomPortal();
    apModeStartTime = millis();
    apModeTimeoutEnabled = true;
}