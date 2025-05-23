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
    constexpr int RESET_BUTTON = 27;    // D4 - User button (was 13, now corrected)    

    // Time of Flight
    constexpr int XSHUT = 34;

    // I2C (BME280 og Time of Flight)
    constexpr int I2C_SDA = 25;
    constexpr int I2C_SCL = 26;
}


#endif