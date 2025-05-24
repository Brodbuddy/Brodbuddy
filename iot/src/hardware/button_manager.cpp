#include "hardware/button_manager.h"

#include "config/constants.h"
#include "config/time_utils.h"

static const char* TAG = "ButtonManager";

ButtonManager::ButtonManager() : buttonPressed(false), buttonPressStart(0), resetRequest(false), portalRequest(false) {}

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
            LOG_W(TAG, "Factory reset initiated");

            for (int i = 0; i < UIConstants::FACTORY_RESET_BLINK_COUNT; i++) {
                digitalWrite(Pins::LED, HIGH);
                TimeUtils::delay_for(TimeConstants::BUTTON_LED_BLINK);
                digitalWrite(Pins::LED, LOW);
                TimeUtils::delay_for(TimeConstants::BUTTON_LED_BLINK);
            }

            resetRequest = true;
        }
    } else {
        if (buttonPressed && millis() - buttonPressStart < TimeUtils::to_ms(TimeConstants::BUTTON_SHORT_PRESS)) {
            LOG_I(TAG, "Starting config portal");

            portalRequest = true;
        }
        buttonPressed = false;
    }
}