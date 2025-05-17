#include "BatteryManager.h"

BatteryManager::BatteryManager() {}

void BatteryManager::begin() {
    analogReadResolution(ADC_RESOLUTION);
}

int BatteryManager::getVoltage() {
    uint32_t adc_reading = 0;
    
    // Brug flere samples for at reducere støj
    for (int i = 0; i < SAMPLES; i++) {
        adc_reading += analogRead(BATTERY_PIN);
        delay(2);
    }
    adc_reading /= SAMPLES;
    
    // Konverter til millivolts
    int voltage = (adc_reading * ADC_REF_VOLTAGE) / ADC_MAX_VALUE;
    voltage *= VOLTAGE_DIVIDER;
    
    return voltage;
}

int BatteryManager::getPercentage() {
    int voltage = getVoltage();
    
    if (voltage >= BATTERY_MAX) return 100;
    if (voltage <= BATTERY_MIN) return 0;
    
    return map(voltage, BATTERY_MIN, BATTERY_MAX, 0, 100);
}

bool BatteryManager::isLow(int thresholdPercentage) {
    return getPercentage() <= thresholdPercentage;
}
