#pragma once

#include <Wire.h>
#include <VL53L0X.h>
#include "i2c_utils.h"

class ToFSensor
{
public:
    ToFSensor();
    bool setup();
    void loop();

    // Getters for sensor data
    int getDistance() const;         // Current distance in mm
    int getInitialHeight() const;    // Initial height in mm
    int getRiseAmount() const;       // Rise amount in mm
    float getRisePercentage() const; // Rise percentage
    float getRiseRate() const;       // Rise rate per hour

    bool isConnected() const;
    bool isRangeValid() const;
    unsigned long getLastReadTime() const;

private:
    VL53L0X sensor;
    bool sensorConnected;
    unsigned long lastReadTime;
    unsigned long startTime;

    // Sensor data
    int currentDistance;  // in mm
    int initialHeight;    // in mm
    int riseAmount;       // in mm
    float risePercentage; // in %
    float riseRate;       // in % per hour
    bool rangeValid;

    int takeMeasurement();
    void scanI2C();
};