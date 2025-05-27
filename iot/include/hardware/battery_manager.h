#ifndef HARDWARE_BATTERY_MANAGER_H
#define HARDWARE_BATTERY_MANAGER_H

#include <Arduino.h>

class BatteryManager {
public:
    BatteryManager();
    void begin();
    int getVoltage();
    int getPercentage();
    bool isLow(int thresholdPercentage = 15);
    bool isCharging();
    bool isCritical();
    bool isSafeForOta() { return getPercentage() >= 20; }

private:
    int readADC();
    void updateChargingState();
    
    static const int VOLTAGE_HISTORY_SIZE = 5;
    int voltageHistory[VOLTAGE_HISTORY_SIZE];
    int historyIndex;
    unsigned long lastVoltageUpdate;
    bool chargingState;
};

#endif