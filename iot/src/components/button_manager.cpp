#include "components/button_manager.h"

static const char* TAG = "ButtonManager";

ButtonManager::ButtonManager() : 
  buttonPressed(false),
  buttonPressStart(0),
  resetRequest(false),
  portalRequest(false) {
}

void ButtonManager::handleButtonPress() {
  if (digitalRead(BUTTON_PIN) == LOW) {
    if (!buttonPressed) {
      buttonPressStart = millis();
      buttonPressed = true;
    } else if (millis() - buttonPressStart > 5000) {
      LOG_W(TAG, "Factory reset initiated");
      
      for (int i = 0; i < 40; i++) {
        digitalWrite(LED_PIN, HIGH);
        delay(50);
        digitalWrite(LED_PIN, LOW);
        delay(50);
      }
      
      resetRequest = true;
    }
  } else {
    if (buttonPressed && millis() - buttonPressStart < 1000) {
      LOG_I(TAG, "Starting config portal");
      
      portalRequest = true;
    }
    buttonPressed = false;
  }
}