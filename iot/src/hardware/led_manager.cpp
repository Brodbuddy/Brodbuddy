#include "hardware/led_manager.h"
#include "config/constants.h"
#include "config/time_utils.h"
#include "logging/logger.h"

#define RGB_LED_PIN 5 

static const char* TAG = "LedManager";

LedManager::LedManager() 
    : currentPattern(OFF), lastUpdate(0), animationStep(0), brightness(30), animationActive(false) {}

void LedManager::begin() {
    FastLED.addLeds<NEOPIXEL, RGB_LED_PIN>(&led, 1);
    FastLED.setBrightness(brightness);
    FastLED.setMaxRefreshRate(60);  
    FastLED.setDither(0);          
    clear();
    LOG_I(TAG, "RGB LED initialized on pin %d, brightness=%d", RGB_LED_PIN, brightness);
}

void LedManager::loop() {
    if (animationActive) {
        updateAnimation();
    }
}

void LedManager::setPattern(Pattern pattern) {
    LOG_I(TAG, "SET_PATTERN: Changing pattern from %d to %d", currentPattern, pattern);
    
    currentPattern = pattern;
    animationStep = 0;
    lastUpdate = millis();
    
    if (pattern == OFF) {
        LOG_I(TAG, "SET_PATTERN: Pattern is OFF, clearing LED");
        clear();
        animationActive = false;
    } else if (pattern == TOF_RESET_CONFIRM || pattern == WIFI_RESET_CONFIRM || pattern == CONNECTED) {
        LOG_I(TAG, "SET_PATTERN: Executing immediate pattern %d", pattern);
        executeImmediatePattern(pattern);
        animationActive = false;
    } else {
        LOG_I(TAG, "SET_PATTERN: Animation active for pattern %d", pattern);
        animationActive = true;
    }
}

void LedManager::setBrightness(uint8_t newBrightness) {
    brightness = newBrightness;
    FastLED.setBrightness(brightness);
}

void LedManager::clear() {
    led = CRGB::Black;
    FastLED.show();
    animationActive = false;
}

void LedManager::executeImmediatePattern(Pattern pattern) {
    switch (pattern) {
        case TOF_RESET_CONFIRM:
            LOG_I(TAG, "Executing TOF_RESET_CONFIRM: 3 orange blinks");
            for (int i = 0; i < 3; i++) {
                showColor(CRGB::Orange);
                TimeUtils::delay_for(std::chrono::milliseconds(200));
                clear();
                TimeUtils::delay_for(std::chrono::milliseconds(200));
            }
            break;
            
        case WIFI_RESET_CONFIRM:
            LOG_I(TAG, "Executing WIFI_RESET_CONFIRM: 3 pink blinks");
            for (int i = 0; i < 3; i++) {
                showColor(CRGB::HotPink);
                TimeUtils::delay_for(std::chrono::milliseconds(200));
                clear();
                TimeUtils::delay_for(std::chrono::milliseconds(200));
            }
            break;
            
        case CONNECTED:
            LOG_I(TAG, "Executing CONNECTED: 3 green blinks");
            for (int i = 0; i < 3; i++) {
                showColor(CRGB::Green);
                TimeUtils::delay_for(std::chrono::milliseconds(200));
                clear();
                TimeUtils::delay_for(std::chrono::milliseconds(200));
            }
            break;
            
        default:
            break;
    }
    currentPattern = OFF;
}

void LedManager::updateAnimation() {
    unsigned long now = millis();
    
    switch (currentPattern) {
            
        case WIFI_CONNECTING:
            breathe(CRGB::Blue, 2000);
            break;
            
        case WIFI_PORTAL:
            blink(CRGB::HotPink, 500, 500, 0);
            break;
            
        case MQTT_CONNECTING:
            blink(CRGB::Blue, 200, 200, 0);
            break;
            
        case OTA_PROGRESS:
            breathe(CRGB::Blue, 1000);
            break;
            
        case BATTERY_LOW:
            blink(CRGB::Red, 200, 1800, 0);
            break;
            
        case ERROR:
            blink(CRGB::Red, 100, 100, 0);
            break;
            
        case OFF:
        default:
            clear();
            break;
    }
}

void LedManager::showColor(CRGB color) {
    led = color;
    FastLED.show();
}

void LedManager::breathe(CRGB color, uint16_t period) {
    unsigned long now = millis();
    float phase = (now % period) / (float)period;
    uint8_t brightness = (sin(phase * 2 * PI) + 1) * 127;
    
    led = color;
    led.fadeToBlackBy(255 - brightness);
    FastLED.show();
}

void LedManager::blink(CRGB color, uint16_t onTime, uint16_t offTime, uint8_t count) {
    unsigned long now = millis();
    static uint8_t blinkCount = 0;
    static bool isOn = false;
    static unsigned long stateStartTime = 0;
    static Pattern lastPattern = OFF;
    static bool initialized = false;
    
    if (currentPattern != lastPattern) {
        blinkCount = 0;
        isOn = false;
        stateStartTime = now;
        lastPattern = currentPattern;
        initialized = false;
    }
    
    if (!initialized) {
        showColor(color);
        isOn = true;
        stateStartTime = now;
        initialized = true;
        return;
    }
    
    unsigned long elapsed = now - stateStartTime;
    
    if (isOn && elapsed >= onTime) {
        clear();
        isOn = false;
        stateStartTime = now;
        blinkCount++;
        
        if (count > 0 && blinkCount >= count) {
            animationActive = false;
            currentPattern = OFF;
            lastPattern = OFF;
            initialized = false;
            clear();
        }
    } else if (!isOn && elapsed >= offTime) {
        if (count == 0 || blinkCount < count) {
            showColor(color);
            isOn = true;
            stateStartTime = now;
        }
    }
}