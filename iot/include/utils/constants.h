#ifndef CONSTANTS_H
#define CONSTANTS_H

#include <Arduino.h>

namespace Pins {
    // EPaper / EInk
    // Busy  -> 4/D12
    // Reset -> 21/SDA
    // D/C   -> 17/D10
    // CS    -> 15/A4
    // SCLK  -> 18/SCK
    // SDI   -> 23/MOSI
    // GND   -> GND
    // VCC   -> 3V3

    constexpr int EINK_BUSY = 4;
    constexpr int EINK_RESET = 21;
    constexpr int EINK_DC = 17;
    constexpr int EINK_CS = 15;
    constexpr int EINK_SCLK = 18;
    constexpr int EINK_SDI = 23; 

    // Led / reset button
    constexpr int LED = 2;              // D9 - Blue/Green onboard LED
    constexpr int RGB_LED = 5;          // D8 - WS2812 RGB LED
    constexpr int RESET_BUTTON = 13; 

    // Time of Flight
    constexpr int XSHUT = 34;

    // I2C (BME280 og Time of Flight)
    constexpr int I2C_SDA = 25;
    constexpr int I2C_SCL = 26;
}

namespace Sensors {
    // I2C Konfiguration
    constexpr int I2C_CLOCK_SPEED = 50000;
    
    // BME280 adresser
    constexpr uint8_t BME280_ADDR_PRIMARY = 0x76;   // SDO -> GND (eller uden SDO sluttet til)
    constexpr uint8_t BME280_ADDR_SECONDARY = 0x77; // SDO -> VCC
    
    // VL53L0X ToF sensor
    constexpr uint8_t VL53L0X_ADDR_DEFAULT = 0x29;
    constexpr int VL53L0X_TIMEOUT_MS = 500;
    constexpr int VL53L0X_TIMING_BUDGET_MS = 200000; // 200ms
    
    // Sensor gyldig range
    constexpr float TEMP_MIN = -40.0f;     // °C 
    constexpr float TEMP_MAX = 85.0f;      // °C
    constexpr float HUMIDITY_MIN = 0.0f;   // %
    constexpr float HUMIDITY_MAX = 100.0f; // %
    constexpr int TOF_DISTANCE_MIN = 20;   // mm 
    constexpr int TOF_DISTANCE_MAX = 2000; // mm 
    
    // Multi-sampling konfiguration
    constexpr int MAX_SAMPLES = 10;
    constexpr int SAMPLE_INTERVAL_MS = 1000;
    constexpr float ALPHA_FILTER = 0.1f;
    constexpr float OUTLIER_THRESHOLD = 2.5f;
    
    // Delays for at stabilisere sensorer
    constexpr int BME280_STABILIZATION_MS = 2000;
    constexpr int VL53L0X_STABILIZATION_MS = 1000;
    constexpr int I2C_INIT_DELAY_MS = 50;
    constexpr int XSHUT_RESET_DELAY_MS = 100;
}

#endif