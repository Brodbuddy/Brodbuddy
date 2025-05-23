#pragma once

#include <WiFi.h>
#include <Preferences.h>
#include "config.h"
#include <utils/constants.h>
#include <utils/logger.h>
#include <data_types.h>
#include "button_manager.h"
#include "captive_portal_manager.h"

#define LED_PIN Pins::LED

class BroadBuddyWiFiManager {
public:
  BroadBuddyWiFiManager();
  void setup();
  void loop();
  WiFiStatus getStatus() const;
  bool resetRequested() const;
  void resetSettings();
  
private:
  Preferences preferences; 
  WiFiStatus currentStatus;
  unsigned long connectStartTime;
  unsigned long previousMillis;
  bool ledState;
  unsigned long lastWiFiCheck;
  
  ButtonManager buttonManager;
  CaptivePortalManager captivePortalManager;
  
  // AP mode timeout variabler
  unsigned long apModeStartTime;
  const unsigned long AP_MODE_TIMEOUT = 300000; // 5 minutter
  bool apModeTimeoutEnabled;
  
  void createBlinkTask();
  void checkWiFiStatus();
  void saveWiFiCredentials(const String& ssid, const String& password);
  
  // Private timeout-kontrolmetoder
  void enableAPModeTimeout();
  void disableAPModeTimeout();
};