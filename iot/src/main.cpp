#include <Arduino.h>
#include "SourdoughDisplay.h"
#include "SourdoughMonitor.h"

SourdoughDisplay display;
SourdoughMonitor monitor(display);

void setup() {
  Serial.begin(115200);
  delay(1000);
  Serial.println("\n\n--- Brodbuddy Sourdough Monitor ---");

  display.begin();
  SourdoughData data = monitor.generateMockData();  
  monitor.updateDisplay(data);
  
  Serial.println("--- Sourdough monitor ready ---");
}

void loop() {
  delay(60000);
}
