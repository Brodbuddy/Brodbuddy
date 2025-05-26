#ifndef SENSOR_MANAGER_H
#define SENSOR_MANAGER_H

#include <Arduino.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include <VL53L0X.h>
#include <Wire.h>

#include "app/data_types.h"
#include "config/constants.h"

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

    bool _firstReading;
    float _filteredTemp;
    float _filteredHum;
    int _baselineDistance;

#ifdef SIMULATE_SENSORS
    float _simTemp;
    float _simHum;
    int _simDistance;
#endif

    float calculateMedian(float arr[], int size);
    int calculateMedianInt(int arr[], int size);
    void removeOutliers(float arr[], int& size, float minVal, float maxVal);
    void removeOutliersInt(int arr[], int& size, int minVal, int maxVal);

  public:
    SensorManager();

    bool begin();
    bool readAllSensors();
    bool collectMultipleSamples();

    const SensorData& getCurrentData() const {
        return _currentData;
    }
    const SensorHealth& getHealth() const {
        return _health;
    }

    void setCalibration(float tempOffset, float humOffset) {
        _tempOffset = tempOffset;
        _humOffset = humOffset;
    }

    void setReadInterval(unsigned long interval) {
        _readInterval = interval;
    }
    bool shouldRead() const;
    
    void scanI2C();
};

#endif