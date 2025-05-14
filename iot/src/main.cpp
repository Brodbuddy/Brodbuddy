#include <Arduino.h>
#include "SourdoughDisplay.h"

SourdoughDisplay display;

void setup() {
  Serial.begin(115200);
  delay(1000);
  Serial.println("\n\n--- ESP32 E-Paper Rettet Orientering Test ---");

  display.begin();
  display.clearBuffers();
  
  // Test af koordinatsystem med en ramme
  display.drawRect(5, 5, display.width()-10, display.height()-10, COLOR_BLACK);
  
  // Test af Adafruit GFX font med korrekt orientering (landskab)
  display.setCursor(40, 30);
  display.setTextColor(COLOR_BLACK);
  display.setTextSize(1);
  display.println("Brodbuddy");
  
  // Rød tekst
  display.setCursor(40, 60);
  display.setTextColor(COLOR_RED);
  display.println("Surdej");
  
  // Temperatur, grad og %
  display.setCursor(40, 90);
  display.setTextColor(COLOR_BLACK);
  display.print("Ude: 21.0");
  display.print("\xB0");
  display.print("C  ");
  display.println("44%");
  
  display.setCursor(40, 120);
  display.print("Inde: 20.7");
  display.print("\xB0");
  display.print("C  ");
  display.println("100%");
  
  // Vækst info
  display.setCursor(40, 150);
  display.setTextColor(COLOR_RED);
  display.print("Vækst: 280%");
  
  // Opdater display
  display.updateDisplay();
  
  Serial.println("--- Test færdig ---");
}

void loop() {
  delay(60000); // Gør intet
}
