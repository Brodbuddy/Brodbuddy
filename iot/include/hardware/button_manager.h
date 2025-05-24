#pragma once

#include <Arduino.h>

#include "config/constants.h"
#include "logging/logger.h"

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