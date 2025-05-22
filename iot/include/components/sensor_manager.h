#ifndef SENSOR_MANAGER_H
#define SENSOR_MANAGER_H

#include <Arduino.h>
#include <VL53L0X.h>
#include <Wire.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include "i2c_utils.h"
#include "data_types.h"

struct SensorHealth {
    bool bme280Connected;
    bool tofConnected;
    unsigned long lastSuccessfulRead;
    int failedReads;
};

class SensorManager {
private:
    Adafruit_BME280 _bme;
    VL53L0X _tof;
    SensorData _currentData;
    SensorHealth _health;

    unsigned long _lastReadTime;
    unsigned long _readInterval;

    float _tempOffset;
    float _humOffset;

#ifdef SIMULATE_SENSORS
    float _simTemp;
    float _simHum;
    int _simDistance;
#endif

public:
    SensorManager();

    bool begin();
    bool readAllSensors();

    const SensorData& getCurrentData() const { return _currentData; }
    const SensorHealth& getHealth() const { return _health; }
   
    void setCalibration(float tempOffset, float humOffset) {
        _tempOffset = tempOffset;
        _humOffset = humOffset;
    }

    void setReadInterval(unsigned long interval) { _readInterval = interval; }
    bool shouldRead() const;
};

#endif