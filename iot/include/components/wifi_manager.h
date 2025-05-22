#pragma once

#include <WiFi.h>
#include <WebServer.h>
#include <DNSServer.h>
#include <Preferences.h>
#include "config.h"

#define LED_PIN 2      // Onboard LED pin 
#define BUTTON_PIN 13  // Onboard button pin

enum WiFiStatus {
  WIFI_DISCONNECTED,
  WIFI_CONNECTING,
  WIFI_CONNECTED
};

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
  String lastConnectionStatus;
  
  // Button handling
  bool buttonPressed;
  unsigned long buttonPressStart;
  bool resetRequest;
  
  // Web server custom portal
  WebServer* server;
  DNSServer* dns;
  bool portalRunning;
  
  // AP mode timeout variabler
  unsigned long apModeStartTime;
  const unsigned long AP_MODE_TIMEOUT = 300000; // 5 minutter
  bool apModeTimeoutEnabled;
  
  void createBlinkTask();
  void handleButtonPress();
  void checkWiFiStatus();
  
  // Custom portal methods
  void startCustomPortal();
  void stopCustomPortal();
  void setupWebServer();
  void handleRoot();
  void handleScan();
  void handleConnect();
  void handleNotFound();
  void handleConnectionStatus(); 
  
  void saveWiFiCredentials(const String& ssid, const String& password);
  
  // Private timeout-kontrolmetoder
  void enableAPModeTimeout();
  void disableAPModeTimeout();
};