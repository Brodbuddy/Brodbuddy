#pragma once

#include <Wire.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include "i2c_utils.h"

class BME280Sensor
{
public:
    BME280Sensor();
    bool setup();
    void loop();

    // Getters for sensor data
    float getTemperature() const;
    float getHumidity() const;
    float getPressure() const;
    float getAltitude() const;

    bool isConnected() const;
    unsigned long getLastReadTime() const;

private:
    Adafruit_BME280 bme;
    bool sensorConnected;
    unsigned long lastReadTime;

    // Sensor data
    float rawTemp;
    float rawHum;
    float filteredTemp;
    float filteredHum;
    float pressure;
    float altitude;

    bool firstReading;

    void readSensor();
    void scanI2C();
};