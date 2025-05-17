#include <Arduino.h>
#include "SourdoughDisplay.h"
#include "SourdoughMonitor.h"
#include "BatteryManager.h"

SourdoughDisplay display;
SourdoughMonitor monitor(display);
BatteryManager battery;
SourdoughData data;

void setup() {
  Serial.begin(115200);
  delay(1000);
  Serial.println("\n\n--- Brodbuddy Sourdough Monitor ---");

  display.begin();
  battery.begin();
  
  data = monitor.generateMockData();
  data.batteryLevel = battery.getPercentage();
  
  monitor.updateDisplay(data);
  
  Serial.println("--- Sourdough monitor ready ---");
}

void loop() {
  data.batteryLevel = battery.getPercentage();  
  monitor.updateDisplay(data);
  delay(60000); // Opdater hvert minut
}
