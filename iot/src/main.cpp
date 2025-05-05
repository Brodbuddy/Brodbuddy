#include <Arduino.h>

const int ledPin = D9;
const int blinkIntervalMs = 1000;

void setup() {
  Serial.begin(115200);
  Serial.println("--- FireBeetle ESP32-E Blink Example ---");
  Serial.print("Configuring onboard LED on pin: ");
  Serial.println(ledPin);

  pinMode(ledPin, OUTPUT);

  Serial.println("Setup complete. Starting loop...");
}

void loop() {
  digitalWrite(ledPin, HIGH);
  Serial.println("LED ON");
  delay(blinkIntervalMs);
  
  digitalWrite(ledPin, LOW);
  Serial.println("LED OFF"); 
  delay(blinkIntervalMs);
}