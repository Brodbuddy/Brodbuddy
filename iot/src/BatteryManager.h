#ifndef BATTERY_MANAGER_H
#define BATTERY_MANAGER_H

#include <Arduino.h>

class BatteryManager {
public:
    BatteryManager();
    void begin();
    int getVoltage();
    int getPercentage();
    bool isLow(int thresholdPercentage = 15);

private:
    static const int BATTERY_PIN = A2;
    static const int SAMPLES = 64;
    static const int BATTERY_MIN = 3300;
    static const int BATTERY_MAX = 4200;
    static constexpr float VOLTAGE_DIVIDER = 2.0;
    
    // ADC konstanter
    static const int ADC_RESOLUTION = 12;
    static const int ADC_MAX_VALUE = 4095;
    static constexpr float ADC_REF_VOLTAGE = 3300.0; // millivolts
};

#endif
