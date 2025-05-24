#include "config.h"

// WiFi
const char *AP_NAME = "BrodBuddy_setup";
const char *AP_PASSWORD = "12345678";
const char *HOSTNAME = "sourdough_monitor";

// MQTT Broker settings
const char *MQTT_SERVER = "15b24181a314453a912a712175056638.s1.eu.hivemq.cloud";
const int MQTT_PORT = 8883;
const char *MQTT_USER = "thom625b";
const char *MQTT_PASSWORD = "Kakao1!!";
const char *DEVICE_ID = "sourdough_monitor_01";

// MQTT Topics
const char *MQTT_TOPIC_BME280 = "devices/sourdough_monitor_01/sensors/bme280";
const char *MQTT_TOPIC_TOF = "devices/sourdough_monitor_01/sensors/tof";
const char *MQTT_STATUS_TOPIC = "devices/sourdough_monitor_01/status";
const char *MQTT_TOPIC_TELEMETRY = "devices/sourdough_monitor_01/telemetry";
const char *MQTT_TOPIC_DHT22 = "devices/sourdough_monitor_01/sensors/dht22"; // New topic for DHT22 sensor

// Backward compatibility - for legacy code
const char *MQTT_TOPIC = "devices/sourdough_monitor_01/data";

// NTP server indstillinger
const char *NTP_SERVER = "pool.ntp.org";
const long GMT_OFFSET_SEC = 3600;     // CET tidszone offset (3600 sekunder = 1 time)
const int DAYLIGHT_OFFSET_SEC = 3600; // Sommertid offset (3600 sekunder = 1 time)

// Time of Flight Sensor
const int TOF_XSHUT_PIN = 34;
const int TOF_NUM_SAMPLES = 5;

// Timing
const unsigned long WIFI_CHECK_INTERVAL = 10000; // Tjek WiFi status hvert 10. sekund
const unsigned long READ_INTERVAL = 60000;       // 1 minut mellem sensoraflæsninger
const unsigned long PUBLISH_INTERVAL = 60000;    // 1 minut mellem MQTT publiceringer
const unsigned long SENSOR_READ_INTERVAL = 2000; // 2 sekunder mellem DHT22 aflæsninger

// LED states
const int LED_BLINK_FAST = 100;
const int LED_BLINK_NORMAL = 500;
const int LED_BLINK_SLOW = 1000;