#ifndef DATA_TYPES_H
#define DATA_TYPES_H

enum WiFiStatus { WIFI_DISCONNECTED, WIFI_CONNECTING, WIFI_CONNECTED };

struct SourdoughReading {
    float temperatureCelsius;
    float humidityPercentage;
    float risePercentange;
    unsigned long timestamp;
};

struct SensorData {
    float inTemp;
    float inHumidity;

    float distanceMillis;

    float currentRisePercent;
    float peakRisePercent;
    float peakHoursAgo;
};

struct HistoricalData {};

#endif