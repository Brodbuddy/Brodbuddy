#include "components/sensor_manager.h"

SensorManager::SensorManager() 
    : _lastReadTime(0),
      _readInterval(15000),
      _tempOffset(0.0f),
      _humOffset(0.0f) {
        memset(&_currentData, 0, sizeof(_currentData));
        memset(&_health, 0, sizeof(_health));

#ifdef SIMULATE_SENSORS
    _simTemp = 22.0f;
    _simHum = 65.0f;
    _simDistance = 500;
#endif
}


bool SensorManager::begin() {
#ifdef SIMULATE_SENSOR
    _health.bme280Connected = true;
    _health.tofConnected = true;
    return true;
#else
    Wire.begin();

    unsigned status = _bme.begin(0x76, &Wire);
    if (!status) {
        status = _bme.begin(0x77, &Wire);
    }

    _health.bme280Connected = status;

    if (_health.bme280Connected) {
        _bme.setSampling(Adafruit_BME280::MODE_NORMAL,
                         Adafruit_BME280::SAMPLING_X16,
                         Adafruit_BME280::SAMPLING_X1,
                         Adafruit_BME280::SAMPLING_X16,
                         Adafruit_BME280::FILTER_X16,
                         Adafruit_BME280::STANDBY_MS_0_5);
        delay(5000);
    }

    _health.tofConnected = _tof.init();
    if (_health.tofConnected) {
        _tof.startContinuous();
    }

    return _health.bme280Connected || _health.tofConnected;
#endif
}

bool SensorManager::readAllSensors() {
    bool success = true;

#ifdef SIMULATE_SENSORS
    _currentData.inTemp = _simTemp;
    _currentData.inHumidity = _simHum;
    _currentData.distanceMillis = _simDistance;
#else
    if (_health.bme280Connected) {
        _currentData.inTemp = _bme.readTemperature() + _tempOffset;
        _currentData.inHumidity = _bme.readHumidity() + _humOffset;
    } else {
        success = false;
        _health.failedReads++;
    }

    if (_health.tofConnected) {
        uint16_t distance = _tof.readRangeContinuousMillimeters();
    
        if (!_tof.timeoutOccurred() && distance > 0 && distance < 2000) {
            _currentData.distanceMillis = distance;
            // current rise
            _currentData.peakRisePercent = max(_currentData.peakRisePercent, _currentData.currentRisePercent);
        } else {
            success = false;
            _health.failedReads++;
        }
    }
#endif

    _lastReadTime = millis();
    return success;
}

bool SensorManager::shouldRead() const {
    return (millis() - _lastReadTime) >= _readInterval;
}