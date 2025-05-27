#include "hardware/sensor_manager.h"

#include "config/constants.h"
#include "config/time_utils.h"
#include "logging/logger.h"

static const char* TAG = "SensorManager";

SensorManager::SensorManager()
    : _lastReadTime(0), _readInterval(15000), _tempOffset(0.0f), _humOffset(0.0f), _firstReading(true),
      _filteredTemp(0.0f), _filteredHum(0.0f), _baselineDistance(0), _loopCallback(nullptr) {
    memset(&_currentData, 0, sizeof(_currentData));
    memset(&_health, 0, sizeof(_health));

#ifdef SIMULATE_SENSORS
    _simTemp = 22.0f;
    _simHum = 65.0f;
    _simDistance = 500;
#endif
}

bool SensorManager::begin() {
#ifdef SIMULATE_SENSORS
    _health.bme280Connected = true;
    _health.tofConnected = true;
    return true;
#else
    LOG_I(TAG, "Initializing sensors...");
    LOG_I(TAG, "I2C pins: SDA=%d, SCL=%d", Pins::I2C_SDA, Pins::I2C_SCL);
    
    Wire.begin(Pins::I2C_SDA, Pins::I2C_SCL);
    TimeUtils::delay_for(TimeConstants::I2C_INIT_DELAY);
    Wire.setClock(Sensors::I2C_CLOCK_SPEED);
    TimeUtils::delay_for(TimeConstants::I2C_INIT_DELAY);
    
    scanI2C();

    LOG_I(TAG, "Attempting to initialize BME280 at address 0x%02X", Sensors::BME280_ADDR_PRIMARY);
    unsigned status = _bme.begin(Sensors::BME280_ADDR_PRIMARY, &Wire);
    if (!status) {
        LOG_W(TAG, "BME280 not found at primary address, trying secondary 0x%02X", Sensors::BME280_ADDR_SECONDARY);
        status = _bme.begin(Sensors::BME280_ADDR_SECONDARY, &Wire);
    }

    _health.bme280Connected = status;

    if (_health.bme280Connected) {
        LOG_I(TAG, "BME280 connected successfully!");
        _bme.setSampling(Adafruit_BME280::MODE_NORMAL, Adafruit_BME280::SAMPLING_X16, Adafruit_BME280::SAMPLING_X1,
                         Adafruit_BME280::SAMPLING_X16, Adafruit_BME280::FILTER_X16, Adafruit_BME280::STANDBY_MS_0_5);
        TimeUtils::delay_for(TimeConstants::BME280_STABILIZATION_DELAY);
        
        float testTemp = _bme.readTemperature();
        float testHum = _bme.readHumidity();
        LOG_I(TAG, "BME280 test read: temp=%.2f°C, hum=%.2f%%", testTemp, testHum);
    } else {
        LOG_E(TAG, "BME280 connection failed!");
    }

    LOG_I(TAG, "Resetting VL53L0X via XSHUT pin %d", Pins::XSHUT);
    pinMode(Pins::XSHUT, OUTPUT);
    digitalWrite(Pins::XSHUT, LOW);
    TimeUtils::delay_for(TimeConstants::XSHUT_RESET_DELAY);
    digitalWrite(Pins::XSHUT, HIGH);
    TimeUtils::delay_for(TimeConstants::XSHUT_RESET_DELAY);

    LOG_I(TAG, "Attempting to initialize VL53L0X at address 0x%02X", Sensors::VL53L0X_ADDR_DEFAULT);
    _health.tofConnected = _tof.init();
    if (_health.tofConnected) {
        LOG_I(TAG, "VL53L0X connected successfully!");
        _tof.setTimeout(TimeUtils::to_ms(TimeConstants::VL53L0X_TIMEOUT));
        _tof.setMeasurementTimingBudget(TimeUtils::to_us(TimeConstants::VL53L0X_TIMING_BUDGET));
        _tof.startContinuous();
        TimeUtils::delay_for(TimeConstants::VL53L0X_STABILIZATION_DELAY);
        
        uint16_t testDist = _tof.readRangeContinuousMillimeters();
        LOG_I(TAG, "VL53L0X test read: distance=%dmm, timeout=%s", testDist, _tof.timeoutOccurred() ? "yes" : "no");
    } else {
        LOG_E(TAG, "VL53L0X connection failed!");
        scanI2C();
    }

    LOG_I(TAG, "Sensor initialization complete. BME280=%s, VL53L0X=%s", 
          _health.bme280Connected ? "connected" : "disconnected",
          _health.tofConnected ? "connected" : "disconnected");

    return _health.bme280Connected || _health.tofConnected;
#endif
}

bool SensorManager::readAllSensors() {
    bool success = true;

#ifdef SIMULATE_SENSORS
    _currentData.inTemp = _simTemp;
    _currentData.inHumidity = _simHum;
    _currentData.distanceMillis = _simDistance;

    if (_firstReading || _baselineDistance == 0) {
        _baselineDistance = _currentData.distanceMillis;
        _currentData.currentRisePercent = 0.0f;
    } else {
        int riseAmount = _baselineDistance - _currentData.distanceMillis;
        _currentData.currentRisePercent = (float)riseAmount / (float)_baselineDistance * 100.0f;
        _currentData.peakRisePercent = max(_currentData.peakRisePercent, _currentData.currentRisePercent);
    }
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

        if (!_tof.timeoutOccurred() && distance > Sensors::TOF_DISTANCE_MIN && distance < Sensors::TOF_DISTANCE_MAX) {
            _currentData.distanceMillis = distance;

            if (_firstReading || _baselineDistance == 0) {
                _baselineDistance = distance;
                _currentData.currentRisePercent = 0.0f;
            } else {
                int riseAmount = _baselineDistance - distance;
                _currentData.currentRisePercent = (float)riseAmount / (float)_baselineDistance * 100.0f;
                _currentData.peakRisePercent = max(_currentData.peakRisePercent, _currentData.currentRisePercent);
            }
        } else {
            success = false;
            _health.failedReads++;
        }
    }
#endif

    _firstReading = false;

    _lastReadTime = millis();
    return success;
}

bool SensorManager::shouldRead() const {
    return (millis() - _lastReadTime) >= _readInterval;
}

void SensorManager::resetBaseline() {
    _baselineDistance = 0;
    _firstReading = true;
    LOG_I(TAG, "ToF baseline reset - next reading will set new baseline");
}

bool SensorManager::collectMultipleSamples() {
#ifdef SIMULATE_SENSORS
    _currentData.inTemp = _simTemp + (random(-10, 10) / 10.0f);
    _currentData.inHumidity = _simHum + (random(-20, 20) / 10.0f);
    
    static int simTime = 0;
    simTime++;
    int simulatedGrowth = 100 + (simTime * 15) + random(-10, 10);
    simulatedGrowth = min(simulatedGrowth, 400);
    
    _currentData.distanceMillis = _simDistance - (simulatedGrowth - 100);
    _currentData.currentRisePercent = simulatedGrowth;
    _currentData.peakRisePercent = max(_currentData.peakRisePercent, _currentData.currentRisePercent);
    
    LOG_D(TAG, "Simulated data: temp=%.1f, hum=%.1f, growth=%d%%", 
          _currentData.inTemp, _currentData.inHumidity, simulatedGrowth);

    _firstReading = false;
    _lastReadTime = millis();
    return true;
#else
    float tempSamples[Sensors::MAX_SAMPLES];
    float humSamples[Sensors::MAX_SAMPLES];
    int distSamples[Sensors::MAX_SAMPLES];
    int validTempSamples = 0;
    int validHumSamples = 0;
    int validDistSamples = 0;

    LOG_D(TAG, "Starting to collect %d samples", Sensors::MAX_SAMPLES);
    LOG_D(TAG, "Sensor status: BME280=%s, VL53L0X=%s", 
          _health.bme280Connected ? "connected" : "disconnected",
          _health.tofConnected ? "connected" : "disconnected");

    for (int i = 0; i < Sensors::MAX_SAMPLES; i++) {
        if (_health.bme280Connected) {
            float temp = _bme.readTemperature() + _tempOffset;
            float hum = _bme.readHumidity() + _humOffset;

            LOG_D(TAG, "BME280 Sample %d: temp=%.2f°C, hum=%.2f%%", i + 1, temp, hum);

            if (temp > Sensors::TEMP_MIN && temp < Sensors::TEMP_MAX) {
                tempSamples[validTempSamples++] = temp;
            } else {
                LOG_W(TAG, "BME280 temp out of range: %.2f°C", temp);
            }
            if (hum >= Sensors::HUMIDITY_MIN && hum <= Sensors::HUMIDITY_MAX) {
                humSamples[validHumSamples++] = hum;
            } else {
                LOG_W(TAG, "BME280 humidity out of range: %.2f%%", hum);
            }
        } else {
            LOG_W(TAG, "BME280 not connected, skipping sample %d", i + 1);
        }

        if (_health.tofConnected) {
            uint16_t dist = _tof.readRangeContinuousMillimeters();
            bool timeout = _tof.timeoutOccurred();
            LOG_D(TAG, "ToF Sample %d: distance=%dmm, timeout=%s", i + 1, dist, timeout ? "yes" : "no");

            if (!timeout && dist > Sensors::TOF_DISTANCE_MIN && dist < Sensors::TOF_DISTANCE_MAX) {
                distSamples[validDistSamples++] = dist;
            } else {
                LOG_W(TAG, "ToF sample invalid: dist=%dmm, timeout=%s", dist, timeout ? "yes" : "no");
            }
        } else {
            LOG_W(TAG, "VL53L0X not connected, skipping sample %d", i + 1);
        }

        if (i < Sensors::MAX_SAMPLES - 1) {
            unsigned long delayStart = millis();
            unsigned long delayDuration = TimeUtils::to_ms(TimeConstants::SENSOR_SAMPLE_INTERVAL);
            
            while (millis() - delayStart < delayDuration) {
                if (_loopCallback) {
                    _loopCallback();
                }
                TimeUtils::delay_for(std::chrono::milliseconds(10));
            }
        }
    }

    LOG_D(TAG, "Collected samples - valid: temp=%d, hum=%d, dist=%d", validTempSamples, validHumSamples,
          validDistSamples);

    if (validTempSamples > 0) {
        removeOutliers(tempSamples, validTempSamples, Sensors::TEMP_MIN, Sensors::TEMP_MAX);
        if (validTempSamples > 0) {
            float medianTemp = calculateMedian(tempSamples, validTempSamples);
            if (_firstReading) {
                _filteredTemp = medianTemp;
            } else {
                _filteredTemp = Sensors::ALPHA_FILTER * medianTemp + (1 - Sensors::ALPHA_FILTER) * _filteredTemp;
            }
            _currentData.inTemp = _filteredTemp;
            LOG_D(TAG, "Temperature result: median=%.2f°C, filtered=%.2f°C", medianTemp, _filteredTemp);
        }
    }

    if (validHumSamples > 0) {
        removeOutliers(humSamples, validHumSamples, Sensors::HUMIDITY_MIN, Sensors::HUMIDITY_MAX);
        if (validHumSamples > 0) {
            float medianHum = calculateMedian(humSamples, validHumSamples);
            if (_firstReading) {
                _filteredHum = medianHum;
            } else {
                _filteredHum = Sensors::ALPHA_FILTER * medianHum + (1 - Sensors::ALPHA_FILTER) * _filteredHum;
            }
            _currentData.inHumidity = _filteredHum;
            LOG_D(TAG, "Humidity result: median=%.2f%%, filtered=%.2f%%", medianHum, _filteredHum);
        }
    }

    if (validDistSamples > 0) {
        removeOutliersInt(distSamples, validDistSamples, Sensors::TOF_DISTANCE_MIN, Sensors::TOF_DISTANCE_MAX);
        if (validDistSamples > 0) {
            int medianDist = calculateMedianInt(distSamples, validDistSamples);
            _currentData.distanceMillis = medianDist;

            if (_firstReading || _baselineDistance == 0) {
                _baselineDistance = medianDist;
                _currentData.currentRisePercent = 100.0f;
                _currentData.peakRisePercent = 100.0f;
                LOG_D(TAG, "Distance baseline set: %dmm, starting at 100%%", _baselineDistance);
            } else {
                int riseAmount = _baselineDistance - medianDist;
                float risePercent = (float)riseAmount / (float)_baselineDistance * 100.0f;
                _currentData.currentRisePercent = 100.0f + risePercent;
                _currentData.peakRisePercent = max(_currentData.peakRisePercent, _currentData.currentRisePercent);
                LOG_D(TAG, "Distance result: median=%dmm, rise=%.2f%%, peak=%.2f%%", medianDist,
                      _currentData.currentRisePercent, _currentData.peakRisePercent);
            }
        }
    }

    _firstReading = false;
    _lastReadTime = millis();
    return (validTempSamples > 0 || validHumSamples > 0 || validDistSamples > 0);
#endif
}

float SensorManager::calculateMedian(float arr[], int size) {
    for (int i = 0; i < size - 1; i++) {
        for (int j = i + 1; j < size; j++) {
            if (arr[i] > arr[j]) {
                float temp = arr[i];
                arr[i] = arr[j];
                arr[j] = temp;
            }
        }
    }

    if (size % 2 == 0) {
        return (arr[size / 2 - 1] + arr[size / 2]) / 2.0f;
    } else {
        return arr[size / 2];
    }
}

int SensorManager::calculateMedianInt(int arr[], int size) {
    for (int i = 0; i < size - 1; i++) {
        for (int j = i + 1; j < size; j++) {
            if (arr[i] > arr[j]) {
                int temp = arr[i];
                arr[i] = arr[j];
                arr[j] = temp;
            }
        }
    }

    if (size % 2 == 0) {
        return (arr[size / 2 - 1] + arr[size / 2]) / 2;
    } else {
        return arr[size / 2];
    }
}

void SensorManager::removeOutliers(float arr[], int& size, float minVal, float maxVal) {
    if (size < 3) return;

    float median = calculateMedian(arr, size);
    float deviations[Sensors::MAX_SAMPLES];
    float totalDev = 0;

    for (int i = 0; i < size; i++) {
        deviations[i] = abs(arr[i] - median);
        totalDev += deviations[i];
    }

    float avgDev = totalDev / size;
    float threshold = avgDev * Sensors::OUTLIER_THRESHOLD;

    int newSize = 0;
    for (int i = 0; i < size; i++) {
        if (deviations[i] <= threshold && arr[i] >= minVal && arr[i] <= maxVal) {
            arr[newSize++] = arr[i];
        }
    }

    size = newSize;
}

void SensorManager::removeOutliersInt(int arr[], int& size, int minVal, int maxVal) {
    if (size < 3) return;

    int median = calculateMedianInt(arr, size);
    int deviations[Sensors::MAX_SAMPLES];
    int totalDev = 0;

    for (int i = 0; i < size; i++) {
        deviations[i] = abs(arr[i] - median);
        totalDev += deviations[i];
    }

    int avgDev = totalDev / size;
    int threshold = avgDev * Sensors::OUTLIER_THRESHOLD;

    int newSize = 0;
    for (int i = 0; i < size; i++) {
        if (deviations[i] <= threshold && arr[i] >= minVal && arr[i] <= maxVal) {
            arr[newSize++] = arr[i];
        }
    }

    size = newSize;
}

void SensorManager::scanI2C() {
    LOG_I(TAG, "Starting I2C scan...");
    uint8_t count = 0;
    
    for (uint8_t address = 1; address < 127; address++) {
        Wire.beginTransmission(address);
        uint8_t error = Wire.endTransmission();
        
        if (error == 0) {
            LOG_I(TAG, "I2C device found at address 0x%02X", address);
            count++;
            
            if (address == Sensors::BME280_ADDR_PRIMARY || address == Sensors::BME280_ADDR_SECONDARY) {
                LOG_I(TAG, "  -> BME280 sensor detected");
            } else if (address == Sensors::VL53L0X_ADDR_DEFAULT) {
                LOG_I(TAG, "  -> VL53L0X sensor detected");
            }
        } else if (error == 4) {
            LOG_E(TAG, "Unknown error at address 0x%02X", address);
        }
    }
    
    if (count == 0) {
        LOG_E(TAG, "No I2C devices found!");
    } else {
        LOG_I(TAG, "I2C scan complete. Found %d device(s)", count);
    }
}