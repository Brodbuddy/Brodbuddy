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

// Buffers for display data (global to be modified by drawing)
uint8_t blackBuffer[EPD_WIDTH * EPD_HEIGHT / 8];
uint8_t redBuffer[EPD_WIDTH * EPD_HEIGHT / 8];

// Function declarations
void sendCommand(uint8_t command);
void sendData(uint8_t data);
void waitUntilIdle();
void fullRefresh();
void setPixel(int x, int y, uint8_t color); // 0:black, 1:white, 2:red

void setup() {
  Serial.begin(115200);
  delay(1000);
  Serial.println("\n\n--- ESP32 E-Paper Test Based on Working Reset Script ---");
  
  pinMode(ELINK_BUSY, INPUT);
  pinMode(ELINK_RESET, OUTPUT);
  pinMode(ELINK_DC, OUTPUT);
  pinMode(ELINK_CS, OUTPUT);
  
  SPI.begin(ELINK_SCLK, -1, ELINK_MOSI, ELINK_CS); // MISO is -1
  SPI.beginTransaction(SPISettings(2000000, MSBFIRST, SPI_MODE0));
  
  Serial.println("Performing extended hardware reset...");
  digitalWrite(ELINK_RESET, HIGH); delay(200);
  digitalWrite(ELINK_RESET, LOW);  delay(500);
  digitalWrite(ELINK_RESET, HIGH); delay(200);
  
  Serial.println("Sending software reset command (0x12)...");
  sendCommand(0x12);  // SWRESET
  waitUntilIdle();
  
  Serial.println("Initializing display with basic commands (from working reset)...");
  sendCommand(0x01); // DRIVER_OUTPUT_CONTROL
  sendData((EPD_HEIGHT - 1) & 0xFF);
  sendData(((EPD_HEIGHT - 1) >> 8) & 0xFF);
  sendData(0x00);    // GD = 0, SM = 0, TB = 0

  sendCommand(0x3C); // BORDER_WAVEFORM_CONTROL
  sendData(0x05);    // Normal border waveform (this might be important for no red border)

  // NEW: Initialize our software buffers to all white / no red
  for (int i = 0; i < EPD_WIDTH * EPD_HEIGHT / 8; i++) {
    blackBuffer[i] = 0xFF; // All white
    redBuffer[i] = 0x00;   // No red (assuming 0x00 is "no red" for red buffer)
  }

  // NEW: Draw a pattern into the software buffers
  Serial.println("Drawing test pattern into buffers...");
  // Draw a black square in the top-left quadrant
  for (int y = 10; y < (EPD_HEIGHT / 4); y++) {
    for (int x = 10; x < (EPD_WIDTH / 2) - 10; x++) {
      setPixel(x, y, 0); // Black
    }
  }
  // Draw a red square in the bottom-right quadrant
  for (int y = (EPD_HEIGHT * 3 / 4); y < EPD_HEIGHT - 10; y++) {
    for (int x = (EPD_WIDTH / 2) + 10; x < EPD_WIDTH - 10; x++) {
      setPixel(x, y, 2); // Red
    }
  }

  Serial.println("Sending buffer data to display RAM...");
  Serial.println("Setting black buffer RAM...");
  sendCommand(0x24);  // WRITE_RAM (black buffer)
  for (int i = 0; i < EPD_WIDTH * EPD_HEIGHT / 8; i++) {
    sendData(blackBuffer[i]);
  }
  
  Serial.println("Setting red buffer RAM...");
  sendCommand(0x26);  // WRITE_RAM_RED
  for (int i = 0; i < EPD_WIDTH * EPD_HEIGHT / 8; i++) {
    sendData(redBuffer[i]);
  }
  
  Serial.println("Updating display with full refresh (using 0xF7 from working reset)...");
  fullRefresh(); // This uses the 0x22/0xF7 sequence from your working reset
  
  // Optional: Power off the display to save image
  // Serial.println("Powering off display...");
  // sendCommand(0x02); // POWER_OFF
  // waitUntilIdle();
  
  Serial.println("--- Test Complete ---");
}

void loop() {
  delay(60000);
}

// E-Paper functions implementation (from your working reset script)
void sendCommand(uint8_t command) {
  digitalWrite(ELINK_DC, LOW);
  digitalWrite(ELINK_CS, LOW);
  SPI.transfer(command);
  digitalWrite(ELINK_CS, HIGH);
}

void sendData(uint8_t data) {
  digitalWrite(ELINK_DC, HIGH);
  digitalWrite(ELINK_CS, LOW);
  SPI.transfer(data);
  digitalWrite(ELINK_CS, HIGH);
}

void waitUntilIdle() {
  // Serial.println("Waiting for display to be ready..."); // Can be noisy
  unsigned long start = millis();
  while(digitalRead(ELINK_BUSY) == LOW) {
    delay(10);
    if (millis() - start > 5000) { // 5 second timeout
        Serial.println("waitUntilIdle TIMEOUT!");
        break;
    }
  }
  // Serial.println("Display is ready.");
}

void fullRefresh() {
  sendCommand(0x22); // DISPLAY_UPDATE_CONTROL_2
  sendData(0xF7);    // Using 0xF7 which your original reset script used
                     // This is different from 0xC7 used in some other attempts
  sendCommand(0x20); // MASTER_ACTIVATION
  waitUntilIdle();
}

// Pixel setting function (for portrait orientation 128x296)
// Assumes x from 0 to 127, y from 0 to 295
void setPixel(int x, int y, uint8_t color) { // 0:black, 1:white, 2:red
  if (x < 0 || x >= EPD_WIDTH || y < 0 || y >= EPD_HEIGHT) {
    return;
  }
  uint16_t byte_index = (x / 8) + (y * (EPD_WIDTH / 8));
  uint8_t bit_pos = 7 - (x % 8);

  if (color == 1) { // White
    blackBuffer[byte_index] |= (1 << bit_pos);
    redBuffer[byte_index] &= ~(1 << bit_pos); // Ensure no red for white
  } else if (color == 0) { // Black
    blackBuffer[byte_index] &= ~(1 << bit_pos);
    redBuffer[byte_index] &= ~(1 << bit_pos); // Ensure no red for black
  } else if (color == 2) { // Red
    blackBuffer[byte_index] |= (1 << bit_pos); // For red, corresponding black buffer bit is white
    redBuffer[byte_index] |= (1 << bit_pos);   // Set red bit
  }
}