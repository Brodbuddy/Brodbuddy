#pragma once

#include <Arduino.h>

// WiFi
extern const char *AP_NAME;
extern const char *AP_PASSWORD;
extern const char *HOSTNAME;

// MQTT Broker settings
extern const char *MQTT_SERVER;
extern const int MQTT_PORT;
extern const char *MQTT_USER;
extern const char *MQTT_PASSWORD;
extern const char *DEVICE_ID;

// Separate topics for different sensor types
extern const char *MQTT_TOPIC_BME280;
extern const char *MQTT_TOPIC_TOF;
extern const char *MQTT_STATUS_TOPIC;
extern const char *MQTT_TOPIC_TELEMETRY;
extern const char *MQTT_TOPIC_DHT22; // New topic for DHT22 sensor

// Backward compatibility - for legacy code
extern const char *MQTT_TOPIC;

// NTP server indstillinger
extern const char *NTP_SERVER;
extern const long GMT_OFFSET_SEC;
extern const int DAYLIGHT_OFFSET_SEC;

// I2C configuration
#define I2C_CLOCK_SPEED 50000

// BME280 Kalibrering
#define TEMP_OFFSET -0.5
#define HUM_OFFSET 1.2
#define ALPHA_TEMP 0.1
#define ALPHA_HUM 0.1

// Time of Flight Sensor
extern const int TOF_XSHUT_PIN;
extern const int TOF_NUM_SAMPLES;

// Timing
extern const unsigned long WIFI_CHECK_INTERVAL;  // Tjek WiFi status hvert 10. sekund
extern const unsigned long READ_INTERVAL;        // Interval mellem sensoraflæsninger
extern const unsigned long PUBLISH_INTERVAL;     // Interval mellem MQTT publiceringer
extern const unsigned long SENSOR_READ_INTERVAL; // Interval mellem DHT22 aflæsninger (2000ms)

// LED states
extern const int LED_BLINK_FAST;
extern const int LED_BLINK_NORMAL;
extern const int LED_BLINK_SLOW;