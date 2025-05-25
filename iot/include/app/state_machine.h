#ifndef STATE_MACHINE_H
#define STATE_MACHINE_H

#include <Arduino.h>

enum AppState {
    STATE_BOOT,
    STATE_CONNECTING_WIFI,
    STATE_CONNECTING_MQTT,
    STATE_SENSING,
    STATE_UPDATING_DISPLAY,
    STATE_PUBLISHING_DATA,
    STATE_SLEEP,
    STATE_OTA_UPDATE,
    STATE_ERROR
};

class StateMachine {
  private:
    AppState _currentState;
    AppState _previousState;
    unsigned long _stateStartTime;
    static const char* _stateNames[];

  public:
    StateMachine();

    AppState getCurrentState() const { return _currentState; }
    AppState getPreviousState() const { return _previousState; }

    void transitionTo(AppState newState);
    unsigned long timeInCurrentState() const;
    const char* getStateName() const;
    const char* getStateName(AppState state) const;

    bool isInState(AppState state) const { return _currentState == state; }
    bool shouldTransition(unsigned long timeThreshold) const;
};

#endif