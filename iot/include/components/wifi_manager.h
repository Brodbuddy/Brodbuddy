#pragma once

#include <WiFi.h>
#include <Preferences.h>
#include "config.h"
#include <utils/constants.h>
#include <utils/logger.h>
#include <data_types.h>
#include "captive_portal_manager.h"

#define LED_PIN Pins::LED

class BroadBuddyWiFiManager {
  public:
    BroadBuddyWiFiManager();
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