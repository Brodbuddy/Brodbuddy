#include "tof_sensor.h"
#include "../include/config.h"

ToFSensor::ToFSensor() : sensorConnected(false),
                         lastReadTime(0),
                         startTime(0),
                         currentDistance(0),
                         initialHeight(0),
                         riseAmount(0),
                         risePercentage(0.0f),
                         riseRate(0.0f),
                         rangeValid(false)
{
}

bool ToFSensor::setup()
{
    // Reset og aktiver sensoren via XSHUT pin
    pinMode(TOF_XSHUT_PIN, OUTPUT);
    digitalWrite(TOF_XSHUT_PIN, LOW); // Deaktiver sensoren
    delay(100);
    digitalWrite(TOF_XSHUT_PIN, HIGH); // Aktiver sensoren
    delay(100);

    // I2C er allerede initialiseret i main.cpp
    Serial.println("Initialiserer VL53L0X Time-of-Flight sensor...");

    // Scan I2C-bus for at tjekke for enheder efter reset
    scanI2CBus();

    // Initalisere VL53L0X sensoren
    Serial.println("Initializing ToF sensor...");
    if (!sensor.init())
    {
        Serial.println("Failed to initialize VL53L0X sensor!");

        // Prøver igen med default addressen
        Serial.println("Trying with default address...");
        sensor.setAddress(0x29);
        if (!sensor.init())
        {
            Serial.println("Still failed to initialize. Check wiring!");
            sensorConnected = false;
            return false;
        }
    }

    // Konfigurer sensoren
    sensor.setTimeout(500);
    sensor.setMeasurementTimingBudget(200000); // 200ms per måling
    sensor.startContinuous();

    sensorConnected = true;
    Serial.println("ToF sensor initialized. Waiting for stabilization...");
    delay(1000);

    // Tag første måling som udgangspunkt
    Serial.println("Taking initial ToF measurement...");
    initialHeight = takeMeasurement();

    Serial.print("Initial distance to surface: ");
    Serial.print(initialHeight);
    Serial.println(" mm");

    if (initialHeight <= 0)
    {
        Serial.println("Invalid initial ToF reading! Sensor might not work correctly.");
        sensorConnected = false;
        return false;
    }

    // Set startTime for beregning af elapsed time
    startTime = millis();
    rangeValid = true;

    return true;
}

void ToFSensor::loop()
{
    if (!sensorConnected)
    {
        return;
    }

    unsigned long currentMillis = millis();
    if (currentMillis - lastReadTime >= READ_INTERVAL)
    {
        // Laver en ny måling
        currentDistance = takeMeasurement();

        if (currentDistance <= 0)
        {
            Serial.println("Invalid ToF reading. Skipping this measurement.");
            rangeValid = false;
        }
        else
        {
            rangeValid = true;
            riseAmount = initialHeight - currentDistance;

            // Beregn stigning i procent
            risePercentage = (riseAmount * 100.0) / initialHeight;

            // Beregn stigningsrate per time
            unsigned long elapsedMinutes = (millis() - startTime) / 60000;
            if (elapsedMinutes > 0)
            {
                riseRate = (risePercentage * 60.0) / elapsedMinutes;
            }

            // Log måling
            unsigned long hours = elapsedMinutes / 60;
            unsigned long mins = elapsedMinutes % 60;

            Serial.println("==============================");
            Serial.print("Time elapsed: ");
            if (hours > 0)
            {
                Serial.print(hours);
                Serial.print("h ");
            }
            Serial.print(mins);
            Serial.println("m");

            Serial.print("ToF distance: ");
            Serial.print(currentDistance);
            Serial.println(" mm");

            Serial.print("Rise amount: ");
            Serial.print(riseAmount);
            Serial.println(" mm");

            Serial.print("Rise percentage: ");
            Serial.print(risePercentage, 1);
            Serial.println("%");

            Serial.print("Rise rate: ");
            Serial.print(riseRate, 1);
            Serial.println("% per hour");
            Serial.println("==============================");
        }

        lastReadTime = currentMillis;
    }
}

int ToFSensor::takeMeasurement()
{
    if (!sensorConnected)
    {
        return -1;
    }

    int sum = 0;
    int validReadings = 0;

    for (int i = 0; i < TOF_NUM_SAMPLES; i++)
    {
        int reading = sensor.readRangeContinuousMillimeters();
        if (!sensor.timeoutOccurred() && reading > 0 && reading < 2000)
        {
            sum += reading;
            validReadings++;
        }
        delay(50);
    }

    if (validReadings == 0)
    {
        Serial.println("All ToF sensor readings failed!");
        return -1;
    }

    return sum / validReadings;
}

void ToFSensor::scanI2C()
{
    // Anvender nu den centraliserede I2C-scanner
    scanI2CBus();
}

int ToFSensor::getDistance() const
{
    return currentDistance;
}

int ToFSensor::getInitialHeight() const
{
    return initialHeight;
}

int ToFSensor::getRiseAmount() const
{
    return riseAmount;
}

float ToFSensor::getRisePercentage() const
{
    return risePercentage;
}

float ToFSensor::getRiseRate() const
{
    return riseRate;
}

bool ToFSensor::isConnected() const
{
    return sensorConnected;
}

bool ToFSensor::isRangeValid() const
{
    return rangeValid;
}

unsigned long ToFSensor::getLastReadTime() const
{
    return lastReadTime;
}