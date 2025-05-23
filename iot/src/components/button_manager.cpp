#include "components/button_manager.h"
#include <utils/time_utils.h>

static const char* TAG = "ButtonManager";

ButtonManager::ButtonManager() : 
  buttonPressed(false),
  buttonPressStart(0),
  resetRequest(false),
  portalRequest(false) {
}

void ButtonManager::begin() {
  pinMode(BUTTON_PIN, INPUT_PULLUP);
}

bool ButtonManager::isStartupResetPressed() const {
  return digitalRead(BUTTON_PIN) == LOW;
}

void ButtonManager::loop() {
  if (digitalRead(BUTTON_PIN) == LOW) {
    if (!buttonPressed) {
      buttonPressStart = millis();
      buttonPressed = true;
    } else if (millis() - buttonPressStart > TimeUtils::to_ms(TimeConstants::BUTTON_LONG_PRESS)) {
      LOG_W(TAG, "Factory reset initiated");
      
      for (int i = 0; i < 40; i++) {
        digitalWrite(LED_PIN, HIGH);
        TimeUtils::delay_for(TimeConstants::BUTTON_LED_BLINK);
        digitalWrite(LED_PIN, LOW);
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