#pragma once

#include <WiFi.h>
#include <WiFiManager.h>

#define LED_PIN 2     // Onboard LED pin
#define BUTTON_PIN 13 // Onboard button pin

enum WiFiStatus
{
    WIFI_DISCONNECTED,
    WIFI_CONNECTING,
    WIFI_CONNECTED
};

class BroadBuddyWiFiManager
{
public:
    BroadBuddyWiFiManager();
    void setup();
    void loop();
    WiFiStatus getStatus() const;
    bool resetRequested() const;
    void resetSettings();

private:
    WiFiManager wifiManager;
    WiFiStatus currentStatus;
    unsigned long connectStartTime;
    unsigned long previousMillis;
    bool ledState;
    unsigned long lastWiFiCheck;

    // Button handling
    bool buttonPressed;
    unsigned long buttonPressStart;
    bool resetRequest;

    void createBlinkTask();
    void handleButtonPress();
    void checkWiFiStatus();
};