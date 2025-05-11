#ifndef SOURDOUGH_DISPLAY_H
#define SOURDOUGH_DISPLAY_H

#include <Arduino.h>
#include <SPI.h>

// Pins for E-paper display 
#define EINK_MISO   -1  // Vi bruger ikke MISO, da e-paper displayet ikke sender data tilbage til ESP32
#define EINK_BUSY    4  // Pin der fortæller om displayet er optaget (LOW = optaget)
#define EINK_RESET  21  // Pin til hardware reset
#define EINK_DC     17  // Data/Command vælger (LOW = kommando, HIGH = data)
#define EINK_CS     15  // Chip Select for SPI kommunikation
#define EINK_SCLK   18  // SPI Clock
#define EINK_MOSI   23  // SPI Data (Master Out Slave In)

// E-paper display opløsning (2.9" = 128x296)
#define EPD_WIDTH  128
#define EPD_HEIGHT 296

// Farve definitioner
#define COLOR_BLACK 0
#define COLOR_WHITE 1
#define COLOR_RED   2

// Display kommandoer
#define CMD_SWRESET              0x12  // Software reset af displayet
#define CMD_DRIVER_OUTPUT        0x01  // Kontrollerer display scanning retning
#define CMD_BORDER_WAVEFORM      0x3C  // Styrer hvordan displayets kant opdateres
#define CMD_WRITE_RAM_BLACK      0x24  // Kommando for at skrive til sort/hvid buffer
#define CMD_WRITE_RAM_RED        0x26  // Kommando for at skrive til rød buffer
#define CMD_DISPLAY_UPDATE       0x22  // Kommando for at starte display opdatering
#define CMD_MASTER_ACTIVATION    0x20  // Aktiverer kommandoen for opdatering

// Parametre til kommandoer
#define PARAM_NORMAL_BORDER      0x05  // Normal kantbølgeform - forhindrer rød kant
#define PARAM_UPDATE_FULL        0xF7  // Fuld opdatering med LUT (Look-Up Table)

// Konfigurationsbits til Driver Output (sendData(0x00) efter CMD_DRIVER_OUTPUT)
#define PARAM_DRIVER_CONFIG      0x00  // GD=0 (Gate driver normal), SM=0 (Scan mod), TB=0 (Scan retning)

class SourdoughDisplay {
public:
    SourdoughDisplay();

    void begin();

    // Nulstil buffer; gør display hvid
    void clearBuffers();

    // Send buffer til display og refresh
    void updateDisplay();

    // TEGNE FUNKTIONER forneden
    void setPixel(int x, int y, uint8_t color);

    void drawTestPattern();

private:
    // Display buffers
    uint8_t blackBuffer[EPD_WIDTH * EPD_HEIGHT / 8];
    uint8_t redBuffer[EPD_WIDTH * EPD_HEIGHT / 8];

    // Hardware control funktioner
    void sendCommand(uint8_t command);
    void sendData(uint8_t data);
    void waitUntilIdle();
    void fullRefresh();
    void hardwareReset();
    void softwareReset();
    void initDisplay();
};

#endif