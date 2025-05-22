#pragma once

#include <WiFiClientSecure.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <time.h>

class MQTTManager
{
public:
    MQTTManager();
    void setup();
    void loop();
    bool isConnected();

    // Sensor-specifikke publiceringsmetoder
    bool publishBME280Data(float temperature, float humidity, float pressure);
    bool publishToFData(int currentDistance, int initialHeight, int riseAmount,
                        float risePercentage, float riseRate);

    // Kombineret telemetri
    bool publishTelemetry(
        // ToF data
        int currentDistance, int initialHeight, int riseAmount, float risePercentage, float riseRate,
        // BME280 data
        float temperature, float humidity, float pressure);

    // Udvidet telemetri med data fra alle sensorer
    bool publishFullTelemetry(
        // ToF data
        int currentDistance, int initialHeight, int riseAmount, float risePercentage, float riseRate,
        // BME280 data
        float bmeTemperature, float bmeHumidity, float pressure);

    // Metoder til at hente seneste modtagne data
    JsonVariant getLastBME280Data();
    JsonVariant getLastToFData();

    // Hjælpe-metode til tidsstempel
    void printLocalTime();

private:
    WiFiClientSecure espClient;
    PubSubClient mqttClient;

    unsigned long lastReconnectAttempt;
    bool ntpConfigured;

    // Buffer for seneste modtagne beskeder
    DynamicJsonDocument lastBME280Data{256};
    DynamicJsonDocument lastToFData{256};

    bool reconnect();
    void configureNTP();
    bool getFormattedTime(char *buffer, size_t bufferSize);

    // Callback funktion til håndtering af indkommende beskeder
    void processMessage(char *topic, byte *payload, unsigned int length);
};