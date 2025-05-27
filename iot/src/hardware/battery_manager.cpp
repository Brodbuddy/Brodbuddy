#include "hardware/battery_manager.h"
#include "config/constants.h"
#include "config/time_utils.h"
#include "logging/logger.h"

static const char* TAG = "BatteryManager";

BatteryManager::BatteryManager() : historyIndex(0), lastVoltageUpdate(0), chargingState(false) {
    for (int i = 0; i < VOLTAGE_HISTORY_SIZE; i++) {
        voltageHistory[i] = 0;
    }
}

void BatteryManager::begin() {
    analogReadResolution(Battery::ADC_RESOLUTION);
    LOG_I(TAG, "Battery manager initialized - Pin: %d, Resolution: %d bits", 
          Pins::BATTERY, Battery::ADC_RESOLUTION);
}

int BatteryManager::readADC() {
    uint32_t adc_reading = 0;
    
    // Brug flere samples for at reducere støj
    for (int i = 0; i < Battery::SAMPLE_COUNT; i++) {
        adc_reading += analogRead(Pins::BATTERY);
        TimeUtils::delay_for(std::chrono::milliseconds(Battery::SAMPLE_DELAY_MS));
    }
    
    return adc_reading / Battery::SAMPLE_COUNT;
}

int BatteryManager::getVoltage() {
    int adc_value = readADC();
    
    // Konverter til millivolts
    int voltage = (adc_value * Battery::ADC_REF_VOLTAGE) / Battery::ADC_MAX_VALUE;
    voltage *= Battery::VOLTAGE_DIVIDER_RATIO;
    
    LOG_D(TAG, "Battery voltage: %dmV (ADC: %d)", voltage, adc_value);
    
    updateChargingState();
    
    return voltage;
}

int BatteryManager::getPercentage() {
    int voltage = getVoltage();
    
    if (voltage >= Battery::MAX_VOLTAGE) return 100;
    if (voltage <= Battery::MIN_VOLTAGE) return 0;
    
    int percentage = map(voltage, Battery::MIN_VOLTAGE, Battery::MAX_VOLTAGE, 0, 100);
    LOG_D(TAG, "Battery percentage: %d%%", percentage);
    
    return percentage;
}

bool BatteryManager::isLow(int thresholdPercentage) {
    bool low = getPercentage() <= thresholdPercentage;
    if (low) {
        LOG_W(TAG, "Low battery detected!");
    }
    return low;
}

bool BatteryManager::isCritical() {
    return getPercentage() <= 10;
}

void BatteryManager::updateChargingState() {
    unsigned long now = millis();
    
    // Opdater kun hvert 30. sekund
    if (now - lastVoltageUpdate < 30000) return;
    
    lastVoltageUpdate = now;
    int currentVoltage = getVoltage();
    
    // Gem voltage i historik
    voltageHistory[historyIndex] = currentVoltage;
    historyIndex = (historyIndex + 1) % VOLTAGE_HISTORY_SIZE;
    
    // Tjek om vi har nok data
    bool hasData = true;
    for (int i = 0; i < VOLTAGE_HISTORY_SIZE; i++) {
        if (voltageHistory[i] == 0) {
            hasData = false;
            break;
        }
    }
    
    if (!hasData) return;
    
    // Beregn gennemsnitlig ændring
    int totalChange = 0;
    int oldestIndex = (historyIndex + 1) % VOLTAGE_HISTORY_SIZE;
    
    for (int i = 1; i < VOLTAGE_HISTORY_SIZE; i++) {
        int currentIdx = (oldestIndex + i) % VOLTAGE_HISTORY_SIZE;
        int prevIdx = (oldestIndex + i - 1) % VOLTAGE_HISTORY_SIZE;
        totalChange += (voltageHistory[currentIdx] - voltageHistory[prevIdx]);
    }
    
    // Hvis voltage stiger med mere end 10mV over perioden, antager vi opladning
    chargingState = (totalChange > 10);
    
    if (chargingState) {
        LOG_I(TAG, "Battery charging detected (voltage trend: +%dmV)", totalChange);
    }
}

bool BatteryManager::isCharging() {
    return chargingState;
}