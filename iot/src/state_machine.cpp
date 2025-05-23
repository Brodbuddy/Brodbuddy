#include "state_machine.h"
#include "utils/logger.h"

const char* StateMachine::_stateNames[] = {
    "BOOT", "CONNECTING_WIFI", "CONNECTING_MQTT", "SENSING", "UPDATING_DISPLAY", "PUBLISHING_DATA", "SLEEP", "ERROR"};

StateMachine::StateMachine() : _currentState(STATE_BOOT), _previousState(STATE_BOOT), _stateStartTime(millis()) {}

void StateMachine::transitionTo(AppState newState) {
    if (_currentState == newState) return;

    _previousState = _currentState;
    _currentState = newState;
    _stateStartTime = millis();

    LOG_I("StateMachine", "Transition: %s -> %s", getStateName(_previousState), getStateName(_currentState));
}

unsigned long StateMachine::timeInCurrentState() const {
    return millis() - _stateStartTime;
}

const char* StateMachine::getStateName() const {
    return getStateName(_currentState);
}

const char* StateMachine::getStateName(AppState state) const {
    if (state >= 0 && state < sizeof(_stateNames) / sizeof(_stateNames[0])) {
        return _stateNames[state];
    }
    return "UNKNOWN";
}

bool StateMachine::shouldTransition(unsigned long timeThreshold) const {
    return timeInCurrentState() >= timeThreshold;
}