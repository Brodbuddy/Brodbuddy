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
    constexpr int LED = 2;     // D9 - Blue/Green onboard LED
    constexpr int RGB_LED = 5; // D8 - WS2812 RGB LED
    constexpr int RESET_BUTTON = 13;

    // Time of Flight
    constexpr int XSHUT = 14;

    // I2C (BME280 og Time of Flight)
    constexpr int I2C_SDA = 25;
    constexpr int I2C_SCL = 26;
}

namespace Sensors {
    // I2C Konfiguration
    constexpr int I2C_CLOCK_SPEED = 10000;

    // BME280 adresser
    constexpr uint8_t BME280_ADDR_PRIMARY = 0x76;   // SDO -> GND (eller uden SDO sluttet til)
    constexpr uint8_t BME280_ADDR_SECONDARY = 0x77; // SDO -> VCC

    // VL53L0X ToF sensor
    constexpr uint8_t VL53L0X_ADDR_DEFAULT = 0x29;

    // Sensor gyldig range
    constexpr float TEMP_MIN = -40.0f;     // °C
    constexpr float TEMP_MAX = 85.0f;      // °C
    constexpr float HUMIDITY_MIN = 0.0f;   // %
    constexpr float HUMIDITY_MAX = 100.0f; // %
    constexpr int TOF_DISTANCE_MIN = 20;   // mm
    constexpr int TOF_DISTANCE_MAX = 2000; // mm

    // Multi-sampling konfiguration
    constexpr int MAX_SAMPLES = 10;
    constexpr float ALPHA_FILTER = 0.1f;
    constexpr float OUTLIER_THRESHOLD = 2.5f;
}

namespace WiFiConstants {
    // Opbevaring af preferences
    constexpr const char* PREFERENCES_NAMESPACE = "wifi";
    constexpr const char* PREF_KEY_SSID = "ssid";
    constexpr const char* PREF_KEY_PASSWORD = "password";

    // Task konfiguration
    constexpr const char* BLINK_TASK_NAME = "BlinkTask";
    constexpr uint32_t BLINK_TASK_STACK_SIZE = 1000;
    constexpr UBaseType_t BLINK_TASK_PRIORITY = 1;
}

namespace NetworkConstants {
    // Standard porte
    constexpr uint16_t HTTP_PORT = 80;
    constexpr uint16_t DNS_PORT = 53;

    // Captive portal router
    constexpr const char* ROUTE_ROOT = "/";
    constexpr const char* ROUTE_SCAN = "/scan";
    constexpr const char* ROUTE_CONNECT = "/connect";
    constexpr const char* ROUTE_STATUS = "/connection-status";

    // Captive portal JSON størrelser
    constexpr size_t PORTAL_JSON_SIZE = 256;
    constexpr size_t PORTAL_CONNECT_JSON_SIZE = 512;
    constexpr size_t PORTAL_SCAN_JSON_SIZE = 4096;

    // Captive portal konfiguration
    constexpr const char* AP_NAME = "BrodBuddy_setup";
    constexpr const char* AP_PASSWORD = "12345678";
    constexpr const char* HOSTNAME = "sourdough_monitor";
    constexpr int WIFI_CONNECT_ATTEMPTS = 30;
    
    constexpr size_t MQTT_BUFFER_SIZE = 8192;
} 

namespace OtaConstants {
    constexpr uint32_t PROGRESS_UPDATE_CHUNK_INTERVAL = 50;
    constexpr uint32_t MQTT_MESSAGE_LOG_INTERVAL = 50;
    constexpr uint32_t CHUNK_LOG_INTERVAL = 10;
    constexpr uint32_t CHUNK_HEADER_SIZE = 8;
    constexpr uint32_t ESTIMATED_TOTAL_CHUNKS = 272;
}

namespace UIConstants {
    constexpr int FACTORY_RESET_BLINK_COUNT = 40;
} 

namespace MonitoringConstants {
    constexpr int MAX_DATA_POINTS = 144; // Maks 12 timer ved 5-minutter intervaller
}

namespace DisplayConstants {
    constexpr int EPD_WIDTH = 128;
    constexpr int EPD_HEIGHT = 296;

    constexpr uint16_t COLOR_BLACK = 0;
    constexpr uint16_t COLOR_WHITE = 1;
    constexpr uint16_t COLOR_RED = 2;

    // Display kommandoer
    constexpr uint8_t CMD_SWRESET = 0x12;           // Software reset af displayet
    constexpr uint8_t CMD_DRIVER_OUTPUT = 0x01;     // Kontrollerer display scanning retning
    constexpr uint8_t CMD_BORDER_WAVEFORM = 0x3C;   // Styrer hvordan displayets kant opdateres
    constexpr uint8_t CMD_WRITE_RAM_BLACK = 0x24;   // Kommando for at skrive til sort/hvid buffer
    constexpr uint8_t CMD_WRITE_RAM_RED = 0x26;     // Kommando for at skrive til rød buffer
    constexpr uint8_t CMD_DISPLAY_UPDATE = 0x22;    // Kommando for at starte display opdatering
    constexpr uint8_t CMD_MASTER_ACTIVATION = 0x20; // Aktiverer kommandoen for opdatering

    // Command parametre
    constexpr uint8_t PARAM_NORMAL_BORDER = 0x05; // Normal kantbølgeform - forhindrer rød kant
    constexpr uint8_t PARAM_UPDATE_FULL = 0xF7;   // Fuld opdatering med LUT (Look-Up Table)
    constexpr uint8_t PARAM_DRIVER_CONFIG = 0x00; // GD=0 (Gate driver normal), SM=0 (Scan mod), TB=0 (Scan retning)
} 

#endif