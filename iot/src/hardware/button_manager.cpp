#include "hardware/button_manager.h"

#include "config/constants.h"
#include "config/time_utils.h"

static const char* TAG = "ButtonManager";

ButtonManager::ButtonManager() : buttonPressed(false), buttonPressStart(0), resetRequest(false), portalRequest(false), tofResetRequest(false) {}

void ButtonManager::begin() {
    pinMode(Pins::RESET_BUTTON, INPUT_PULLUP);
}

bool ButtonManager::isStartupResetPressed() const {
    return digitalRead(Pins::RESET_BUTTON) == LOW;
}

void ButtonManager::loop() {
    if (digitalRead(Pins::RESET_BUTTON) == LOW) {
        if (!buttonPressed) {
            buttonPressStart = millis();
            buttonPressed = true;
        } else if (millis() - buttonPressStart > TimeUtils::to_ms(TimeConstants::BUTTON_LONG_PRESS)) {
            LOG_W(TAG, "WiFi reset initiated");
            resetRequest = true;
        }
    } else {
        if (buttonPressed) {
            unsigned long pressDuration = millis() - buttonPressStart;
            if (pressDuration < TimeUtils::to_ms(TimeConstants::BUTTON_SHORT_PRESS)) {
                LOG_I(TAG, "ToF reset requested");
                tofResetRequest = true;
            } else if (pressDuration < TimeUtils::to_ms(TimeConstants::BUTTON_LONG_PRESS)) {
                LOG_I(TAG, "Starting config portal");
                portalRequest = true;
            }
        }
        buttonPressed = false;
    }
}