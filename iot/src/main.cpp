#include <Arduino.h>
#include <SPI.h>

// Define pins for the E-Paper display
#define ELINK_BUSY 4
#define ELINK_RESET 21
#define ELINK_DC 17
#define ELINK_CS 15
#define ELINK_SCLK 18
#define ELINK_MOSI 23

// Display resolution (2.9" = 128x296)
#define EPD_WIDTH       128
#define EPD_HEIGHT      296

// Function declarations for E-Paper functions
void sendCommand(uint8_t command);
void sendData(uint8_t data);
void waitUntilIdle();
void clearBuffers();
void fullRefresh();

void setup() {
  Serial.begin(115200);
  delay(1000);
  Serial.println("\n\n--- ESP32 E-Paper Total Reset ---");
  
  // Initialize E-Paper pins
  pinMode(ELINK_BUSY, INPUT);
  pinMode(ELINK_RESET, OUTPUT);
  pinMode(ELINK_DC, OUTPUT);
  pinMode(ELINK_CS, OUTPUT);
  
  // Initialize SPI
  SPI.begin(ELINK_SCLK, -1, ELINK_MOSI, ELINK_CS);
  SPI.beginTransaction(SPISettings(2000000, MSBFIRST, SPI_MODE0));
  
  // Step 1: Extended hardware reset
  Serial.println("Performing extended hardware reset...");
  digitalWrite(ELINK_RESET, HIGH);
  delay(200);
  digitalWrite(ELINK_RESET, LOW);
  delay(500);  // Longer low period
  digitalWrite(ELINK_RESET, HIGH);
  delay(200);
  
  // Step 2: Software reset
  Serial.println("Sending software reset command...");
  sendCommand(0x12);  // SWRESET
  waitUntilIdle();
  
  // Step 3: Full initialization sequence with different approach
  Serial.println("Initializing display with basic commands...");
  sendCommand(0x01); // DRIVER_OUTPUT_CONTROL
  sendData((EPD_HEIGHT - 1) & 0xFF);
  sendData(((EPD_HEIGHT - 1) >> 8) & 0xFF);
  sendData(0x00);    // GD = 0, SM = 0, TB = 0

  sendCommand(0x3C); // BORDER_WAVEFORM_CONTROL
  sendData(0x05);    // Normal border waveform

  // Step 4: Try to clear both black and red buffers 
  Serial.println("Clearing both black and red buffers...");
  
  // First, clear the black buffer to white
  Serial.println("Setting black buffer to all white...");
  sendCommand(0x24);  // WRITE_RAM (black buffer)
  for (int i = 0; i < EPD_WIDTH * EPD_HEIGHT / 8; i++) {
    sendData(0xFF);  // All white
  }
  
  // Then, clear the red buffer (no red)
  Serial.println("Setting red buffer to all white (no red)...");
  sendCommand(0x26);  // WRITE_RAM_RED
  for (int i = 0; i < EPD_WIDTH * EPD_HEIGHT / 8; i++) {
    sendData(0x00);  // No red
  }
  
  // Update the display
  Serial.println("Updating display with full refresh...");
  fullRefresh();
  
  // Wait for the display to settle
  delay(3000);
  
  // Step 5: Alternative fallback attempt (try another command set)
  Serial.println("Trying alternative command set...");
  
  sendCommand(0x04); // POWER_ON
  waitUntilIdle();   
  
  sendCommand(0x02); // POWER_OFF
  waitUntilIdle();
  
  sendCommand(0x06); // BOOSTER_SOFT_START
  sendData(0x17);
  sendData(0x17);
  sendData(0x17);
  
  sendCommand(0x00); // PANEL_SETTING
  sendData(0x0F);    // LUT from register, B/W mode
  
  sendCommand(0x61); // RESOLUTION_SETTING
  sendData(EPD_WIDTH);
  sendData(EPD_HEIGHT >> 8);
  sendData(EPD_HEIGHT & 0xFF);
  
  // Try one more full refresh
  Serial.println("Performing second full refresh...");
  clearBuffers();
  fullRefresh();
  
  // Final state - power off
  Serial.println("Powering off display...");
  sendCommand(0x02); // POWER_OFF
  
  Serial.println("Reset sequence complete. Display should now be cleared.");
}

void loop() {
  // Nothing to do in loop
  delay(1000);
}

// E-Paper functions implementation
void sendCommand(uint8_t command) {
  digitalWrite(ELINK_DC, LOW);  // Command mode
  digitalWrite(ELINK_CS, LOW);  // Select chip
  SPI.transfer(command);
  digitalWrite(ELINK_CS, HIGH); // Deselect chip
}

void sendData(uint8_t data) {
  digitalWrite(ELINK_DC, HIGH); // Data mode
  digitalWrite(ELINK_CS, LOW);  // Select chip
  SPI.transfer(data);
  digitalWrite(ELINK_CS, HIGH); // Deselect chip
}

void waitUntilIdle() {
  Serial.println("Waiting for display to be ready...");
  while(digitalRead(ELINK_BUSY) == LOW) {
    delay(10);
  }
  Serial.println("Display is ready.");
}

void clearBuffers() {
  // Clear black buffer (all white)
  sendCommand(0x24);  // WRITE_RAM
  for (int i = 0; i < EPD_WIDTH * EPD_HEIGHT / 8; i++) {
    sendData(0xFF);  // All white
  }
  
  // Clear red buffer (no red)
  sendCommand(0x26);  // WRITE_RAM_RED
  for (int i = 0; i < EPD_WIDTH * EPD_HEIGHT / 8; i++) {
    sendData(0x00);  // No red
  }
}

void fullRefresh() {
  sendCommand(0x22); // DISPLAY_UPDATE_CONTROL_2
  sendData(0xF7);    // Full refresh, load LUT
  
  sendCommand(0x20); // MASTER_ACTIVATION
  waitUntilIdle();
}