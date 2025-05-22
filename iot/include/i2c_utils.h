#pragma once

#include <Arduino.h>
#include <Wire.h>

// Centraliseret I2C scanning funktion
void scanI2CBus();

// Initialisering af I2C-bus én gang
void initI2C();