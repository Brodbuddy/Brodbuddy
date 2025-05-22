#include "i2c_utils.h"
#include "../include/config.h"
#include <Arduino.h>
#include <Wire.h>

// Centraliseret I2C scanning funktion
void scanI2CBus()
{
    Serial.println("\n========= I2C Scanner =========");
    Serial.printf("SDA: GPIO%d, SCL: GPIO%d, Clock: %d Hz\n", I2C_SDA, I2C_SCL, I2C_CLOCK_SPEED);

    byte error, address;
    int nDevices = 0;

    // Scanner adresser fra 1-127
    for (address = 1; address < 127; address++)
    {
        Wire.beginTransmission(address);
        error = Wire.endTransmission();

        if (error == 0)
        {
            Serial.print("I2C enhed fundet på adresse 0x");
            if (address < 16)
                Serial.print("0");
            Serial.print(address, HEX);

            // Identificer kendte enheder
            if (address == 0x76 || address == 0x77)
            {
                Serial.print(" - Muligvis BME280/BMP280");
            }
            else if (address == 0x29)
            {
                Serial.print(" - Muligvis VL53L0X Time-of-Flight");
            }

            Serial.println();
            nDevices++;
        }
        else if (error == 4)
        {
            Serial.print("Ukendt fejl på adresse 0x");
            if (address < 16)
                Serial.print("0");
            Serial.println(address, HEX);
        }
    }

    if (nDevices == 0)
    {
        Serial.println("*** INGEN I2C ENHEDER FUNDET! Tjek forbindelserne. ***");
    }
    else
    {
        Serial.print("Fandt ");
        Serial.print(nDevices);
        Serial.println(" I2C-enheder");
    }
    Serial.println("================================\n");
}

// Initialisering af I2C-bus én gang
void initI2C()
{
    Wire.begin(I2C_SDA, I2C_SCL);
    delay(50);
    Wire.setClock(I2C_CLOCK_SPEED);
    delay(50);
    Serial.printf("I2C initialiseret: SDA=%d, SCL=%d, Hastighed=%d Hz\n",
                  I2C_SDA, I2C_SCL, I2C_CLOCK_SPEED);
}