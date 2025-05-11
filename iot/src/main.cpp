#include <Arduino.h>
#include "SourdoughDisplay.h"

SourdoughDisplay display;

void setup() {
  Serial.begin(115200);
  delay(1000);
  Serial.println("\n\n--- ESP32 E-Paper Test Based on Working Reset Script ---");

  display.begin();
  display.clearBuffers();
  display.drawString(10, 10, "Benny", COLOR_BLACK);
  display.drawString(10, 25, "Temp: 22.5C", COLOR_RED);
  display.drawString(10, 40, "Rise: 35%", COLOR_BLACK);
  display.updateDisplay();
  
  Serial.println("--- Test Complete ---");
}

void loop() {
  delay(60000); // Gør intet
}