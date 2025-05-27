#ifndef HARDWARE_LED_MANAGER_H
#define HARDWARE_LED_MANAGER_H

#include <Arduino.h>
#include <FastLED.h>

class LedManager {
public:
    enum Pattern {
        OFF,                   // Ingen lys
        TOF_RESET_CONFIRM,     // 3 korte ORANGE blink (ToF nulstilling bekræftet)
        WIFI_RESET_CONFIRM,    // 3 korte PINK blink (WiFi nulstilling bekræftet) 
        WIFI_CONNECTING,       // Langsom BLÅ puls (forbinder til WiFi)
        WIFI_PORTAL,           // Gentagne PINK blink (captive portal aktiv)
        MQTT_CONNECTING,       // Hurtige BLÅ blink (forbinder til MQTT)
        CONNECTED,             // 3 korte GRØNNE blink (forbundet succesfuldt)
        OTA_PROGRESS,          // BLÅ fremskridt (OTA opdatering)
        BATTERY_LOW,           // Langsom RØD blink (lavt batteri)
        ERROR                  // Hurtige RØDE blink (generel fejl)
    };

    LedManager();
    void begin();
    void loop();
    void setPattern(Pattern pattern);
    void setBrightness(uint8_t brightness);
    void clear();
    Pattern getCurrentPattern() const { return currentPattern; }

private:
    CRGB led;
    Pattern currentPattern;
    unsigned long lastUpdate;
    uint8_t animationStep;
    uint8_t brightness;
    bool animationActive;
    
    void updateAnimation();
    void executeImmediatePattern(Pattern pattern);
    void showColor(CRGB color);
    void breathe(CRGB color, uint16_t period);
    void blink(CRGB color, uint16_t onTime, uint16_t offTime, uint8_t count = 0);
};

#endif