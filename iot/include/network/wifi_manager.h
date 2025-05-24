#pragma once

#include <WiFi.h>
#include <Preferences.h>

#include "app/data_types.h"
#include "config/constants.h"
#include "logging/logger.h"
#include "network/captive_portal_manager.h"

class WifiManager {
  public:
    WifiManager();
    void begin();
    void loop();
    WiFiStatus getStatus() const;
    void resetSettings();
    bool hasError() const;
    void startCaptivePortal();

  private:
    Preferences preferences;
    WiFiStatus currentStatus;
    unsigned long connectStartTime;
    unsigned long previousMillis;
    bool ledState;
    unsigned long lastWiFiCheck;

    CaptivePortalManager captivePortalManager;

    unsigned long apModeStartTime;
    bool apModeTimeoutEnabled;
    bool apModeTimeoutOccurred;

    void createBlinkTask();
    void checkWiFiStatus();
    void saveWiFiCredentials(const String& ssid, const String& password);

    void enableAPModeTimeout();
    void disableAPModeTimeout();
};