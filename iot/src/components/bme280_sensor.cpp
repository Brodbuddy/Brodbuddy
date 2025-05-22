#include "components/bme280_sensor.h"
#include "config.h"

BME280Sensor::BME280Sensor() : sensorConnected(false),
                               lastReadTime(0),
                               rawTemp(0.0f),
                               rawHum(0.0f),
                               filteredTemp(0.0f),
                               filteredHum(0.0f),
                               pressure(0.0f),
                               altitude(0.0f),
                               firstReading(true)
{
}

bool BME280Sensor::setup()
{
    // I2C-bussen er allerede initialiseret i main.cpp

    // BME280 initialisering og konfiguration
    Serial.println("Forsøger at initialisere BME280 sensor...");

    // Try to initialize the BME280 sensor
    unsigned status = bme.begin(0x76, &Wire); // I2C address is usually 0x76 or 0x77

    if (!status)
    {
        Serial.println("Could not find a valid BME280 sensor, check wiring, address, sensor ID!");
        Serial.print("SensorID was: 0x");
        Serial.println(bme.sensorID(), 16);
        sensorConnected = false;

        // Try alternate address
        status = bme.begin(0x77, &Wire);
        if (!status)
        {
            Serial.println("Still can't find BME280 at alternate address");
            return false;
        }
        else
        {
            Serial.println("Found BME280 at alternate address 0x77");
        }
    }
    else
    {
        Serial.println("BME280 fundet på adresse 0x76");
    }

    sensorConnected = true;

    // Konfigurer for maksimal præcision
    bme.setSampling(Adafruit_BME280::MODE_NORMAL,
                    Adafruit_BME280::SAMPLING_X16,
                    Adafruit_BME280::SAMPLING_X1,
                    Adafruit_BME280::SAMPLING_X16,
                    Adafruit_BME280::FILTER_X16,
                    Adafruit_BME280::STANDBY_MS_0_5);

    Serial.println("BME280 konfigureret for maksimal præcision");

    // Lad sensoren stabilisere sig
    Serial.println("Lader sensoren stabilisere sig...");
    for (int i = 5; i > 0; i--)
    {
        Serial.print(i);
        Serial.println(" sekunder tilbage...");
        delay(1000);
    }

    // Tag første aflæsninger
    rawTemp = bme.readTemperature();
    rawHum = bme.readHumidity();

    if (isnan(rawTemp) || isnan(rawHum))
    {
        Serial.println("Fejl i første læsning!");
        return false;
    }

    filteredTemp = rawTemp + TEMP_OFFSET;
    filteredHum = rawHum + HUM_OFFSET;
    firstReading = false;

    return true;
}

void BME280Sensor::loop()
{
    if (!sensorConnected)
    {
        return;
    }

    unsigned long currentMillis = millis();
    if (currentMillis - lastReadTime >= READ_INTERVAL)
    {
        readSensor();
    }
}

void BME280Sensor::readSensor()
{
    if (!sensorConnected)
    {
        return;
    }

    rawTemp = bme.readTemperature();
    rawHum = bme.readHumidity();
    pressure = bme.readPressure() / 100.0F; // Convert Pa to hPa
    altitude = bme.readAltitude(1013.25);   // Standard pressure at sea level

    // Validér læsninger
    if (isnan(rawTemp) || rawTemp < -40 || rawTemp > 85)
    {
        Serial.println("Fejl i temperaturlæsning!");
        return;
    }

    if (isnan(rawHum) || rawHum < 0 || rawHum > 100)
    {
        Serial.println("Fejl i fugtighedslæsning!");
        return;
    }

    // Tilføj offset
    rawTemp += TEMP_OFFSET;
    rawHum += HUM_OFFSET;

    // EWMA filter
    if (firstReading)
    {
        filteredTemp = rawTemp;
        filteredHum = rawHum;
        firstReading = false;
    }
    else
    {
        filteredTemp = (ALPHA_TEMP * rawTemp) + ((1 - ALPHA_TEMP) * filteredTemp);
        filteredHum = (ALPHA_HUM * rawHum) + ((1 - ALPHA_HUM) * filteredHum);
    }

    lastReadTime = millis();

    Serial.print("Temperatur: ");
    Serial.print(filteredTemp, 2);
    Serial.print(" °C, Luftfugtighed: ");
    Serial.print(filteredHum, 2);
    Serial.print(" %, Tryk: ");
    Serial.print(pressure);
    Serial.println(" hPa");
}

void BME280Sensor::scanI2C()
{
    // Anvender nu den centraliserede I2C-scanner
    scanI2CBus();
}

float BME280Sensor::getTemperature() const
{
    return filteredTemp;
}

float BME280Sensor::getHumidity() const
{
    return filteredHum;
}

float BME280Sensor::getPressure() const
{
    return pressure;
}

float BME280Sensor::getAltitude() const
{
    return altitude;
}

bool BME280Sensor::isConnected() const
{
    return sensorConnected;
}

unsigned long BME280Sensor::getLastReadTime() const
{
    return lastReadTime;
}