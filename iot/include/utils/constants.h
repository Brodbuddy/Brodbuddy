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
    constexpr int LED = 2;
    constexpr int RESET_BUTTON = 13;    

    // Time of Flight
    constexpr int XSHUT = 34;

    // I2C (BME280 og Time of Flight)
    constexpr int I2C_SDA = 25;
    constexpr int I2C_SCL = 26;
}


#endif