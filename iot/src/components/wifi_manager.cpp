#include "components/wifi_manager.h"
#include <Arduino.h>
#include <utils/constants.h>
#include <utils/logger.h>

static const char* TAG = "WiFiManager";

#define BLINK_INTERVAL LED_BLINK_NORMAL

TaskHandle_t blinkTaskHandle = NULL;

BroadBuddyWiFiManager::BroadBuddyWiFiManager() : 
  currentStatus(WIFI_DISCONNECTED),
  connectStartTime(0),
  previousMillis(0),
  ledState(LOW),
  lastWiFiCheck(0),
  apModeStartTime(0),
  apModeTimeoutEnabled(true) {
}

void BroadBuddyWiFiManager::setup() {
  pinMode(LED_PIN, OUTPUT);
  pinMode(BUTTON_PIN, INPUT_PULLUP);
  
  if (digitalRead(BUTTON_PIN) == LOW) {
    LOG_I(TAG, "Reset button pressed during startup - clearing WiFi settings");
    resetSettings();
    delay(1000);
    ESP.restart();
  }
  
  currentStatus = WIFI_CONNECTING;
  ledState = LOW;
  digitalWrite(LED_PIN, ledState);
  connectStartTime = millis();
  
  createBlinkTask();
  
  LOG_I(TAG, "Attempting to connect with saved credentials");
  
  preferences.begin("wifi", false);
  String ssid = preferences.getString("ssid", "");
  String password = preferences.getString("password", "");
  preferences.end();
  
  if (ssid.length() > 0) {
    LOG_I(TAG, "Found saved SSID: %s", ssid.c_str());
    WiFi.begin(ssid.c_str(), password.c_str());
  } else {
    WiFi.begin();
  }
  
  int timeout = 10; // 10 seconds timeout
  while (WiFi.status() != WL_CONNECTED && timeout > 0) {
    delay(1000);
    LOG_D(TAG, "Connecting...");
    timeout--;
  }
  
  if (WiFi.status() == WL_CONNECTED) {
    LOG_I(TAG, "Connected to WiFi!");
    LOG_I(TAG, "IP address: %s", WiFi.localIP().toString().c_str());
    
    // Set hostname for mDNS
    WiFi.setHostname(HOSTNAME);
    
    currentStatus = WIFI_CONNECTED;
    digitalWrite(LED_PIN, HIGH);
  } else {
    // If connection fails, start the custom portal
    LOG_W(TAG, "Failed to connect with saved credentials, starting custom portal");
    captivePortalManager.setSaveCredentialsCallback([this](const String& ssid, const String& password) {
      this->saveWiFiCredentials(ssid, password);
    });
    captivePortalManager.setStatusCallback([this](int status) {
      this->currentStatus = (WiFiStatus)status;
    });
    captivePortalManager.setBlinkTaskCallback([this]() {
      this->createBlinkTask();
    });
    captivePortalManager.startCustomPortal();
    apModeStartTime = millis();
  }
}

void BroadBuddyWiFiManager::loop() {
  // Handle captive portal if running
  captivePortalManager.loop();
  
  // Check for AP mode timeout if portal is running
  if (captivePortalManager.isRunning()) {
    if (apModeTimeoutEnabled && (millis() - apModeStartTime > AP_MODE_TIMEOUT)) {
      LOG_W(TAG, "AP mode timeout - restarting device");
      delay(500);
      ESP.restart();
    }
  }

  // Update status based on actual WiFi state
  wl_status_t wifiStatus = WiFi.status();
  if (wifiStatus == WL_CONNECTED && currentStatus != WIFI_CONNECTED) {
    currentStatus = WIFI_CONNECTED;
  } else if (wifiStatus != WL_CONNECTED && currentStatus == WIFI_CONNECTED) {
    currentStatus = WIFI_DISCONNECTED;
  }

  // LED handling
  if (currentStatus == WIFI_CONNECTED) {
    // Solid on when connected
    digitalWrite(LED_PIN, HIGH);
  } else if (currentStatus == WIFI_CONNECTING && !captivePortalManager.isRunning()) {
    // Blink when connecting (but not when portal is active, because we have a task for that)
    unsigned long currentMillis = millis();
    if (currentMillis - previousMillis >= BLINK_INTERVAL) {
      previousMillis = currentMillis;
      ledState = !ledState;
      digitalWrite(LED_PIN, ledState);
    }
  }
  
  // Button handling
  buttonManager.handleButtonPress();
  
  if (buttonManager.isResetRequested()) {
    resetSettings();
    buttonManager.clearResetRequest();
    ESP.restart();
  }
  
  if (buttonManager.isPortalRequested()) {
    buttonManager.clearPortalRequest();
    captivePortalManager.setSaveCredentialsCallback([this](const String& ssid, const String& password) {
      this->saveWiFiCredentials(ssid, password);
    });
    captivePortalManager.setStatusCallback([this](int status) {
      this->currentStatus = (WiFiStatus)status;
    });
    captivePortalManager.setBlinkTaskCallback([this]() {
      this->createBlinkTask();
    });
    captivePortalManager.startCustomPortal();
    apModeStartTime = millis();
  }
  
  // Check WiFi status
  checkWiFiStatus();
}

void BroadBuddyWiFiManager::createBlinkTask() {
  // Only create task if one doesn't already exist
  if (blinkTaskHandle != NULL) {
    vTaskDelete(blinkTaskHandle);
    blinkTaskHandle = NULL;
  }
  
  xTaskCreate(
    [](void* parameter) {
      bool localLedState = false;
      while (true) {
        localLedState = !localLedState;
        digitalWrite(LED_PIN, localLedState ? HIGH : LOW);
        vTaskDelay(BLINK_INTERVAL / portTICK_PERIOD_MS);
      }
    },
    "BlinkTask",
    1000,
    NULL,
    1,
    &blinkTaskHandle
  );
}

void BroadBuddyWiFiManager::checkWiFiStatus() {
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

void BroadBuddyWiFiManager::saveWiFiCredentials(const String& ssid, const String& password) {
  preferences.begin("wifi", false);
  preferences.putString("ssid", ssid);
  preferences.putString("password", password);
  preferences.end();
}

void BroadBuddyWiFiManager::resetSettings() {
  preferences.begin("wifi", false);
  preferences.clear();
  preferences.end();
  
  WiFi.disconnect(true);
}

void BroadBuddyWiFiManager::enableAPModeTimeout() {
  apModeTimeoutEnabled = true;
  apModeStartTime = millis();
}

void BroadBuddyWiFiManager::disableAPModeTimeout() {
  apModeTimeoutEnabled = false;
}

WiFiStatus BroadBuddyWiFiManager::getStatus() const {
  return currentStatus;
}

bool BroadBuddyWiFiManager::resetRequested() const {
  return buttonManager.isResetRequested();
}