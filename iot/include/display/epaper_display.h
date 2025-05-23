#ifndef EPAPER_DISPLAY_H
#define EPAPER_DISPLAY_H

#include <Arduino.h>
#include <SPI.h>
#include <Adafruit_GFX.h>
#include <utils/constants.h>


class EpaperDisplay : public Adafruit_GFX {
  public:
    EpaperDisplay();

    void begin();

    // Nulstil buffer; gør display hvid
    void clearBuffers();

    // Send buffer til display og refresh
    void updateDisplay();

    // GFX implementation
    void drawPixel(int16_t x, int16_t y, uint16_t color) override;

    void drawTestPattern();

  private:
    // Display buffers
    uint8_t blackBuffer[DisplayConstants::EPD_WIDTH * DisplayConstants::EPD_HEIGHT / 8];
    uint8_t redBuffer[DisplayConstants::EPD_WIDTH * DisplayConstants::EPD_HEIGHT / 8];

    // Hardware control funktioner
    void sendCommand(uint8_t command);
    void sendData(uint8_t data);
    void waitUntilIdle();
    void fullRefresh();
    void hardwareReset();
    void softwareReset();
    void initDisplay();
    void setPixel(int16_t x, int16_t y, uint16_t color);
};

#endif