#pragma once

#include <Arduino.h>
#include <utils/constants.h>
#include <utils/logger.h>

#define LED_PIN Pins::LED    
#define BUTTON_PIN Pins::RESET_BUTTON 

class ButtonManager {
public:
  ButtonManager();
  void begin();
  bool isStartupResetPressed() const;
  void loop();
  bool isResetRequested() const { return resetRequest; }
  void clearResetRequest() { resetRequest = false; }
  bool isPortalRequested() const { return portalRequest; }
  void clearPortalRequest() { portalRequest = false; }
  
private:
  bool buttonPressed;
  unsigned long buttonPressStart;
  bool resetRequest;
  bool portalRequest;
};