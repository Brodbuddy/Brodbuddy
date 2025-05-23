#include "display/epaper_display.h"
#include "utils/constants.h"
#include "utils/logger.h"
#include "utils/time_utils.h"

static const char* TAG = "EpaperDisplay";

EpaperDisplay::EpaperDisplay() : Adafruit_GFX(DisplayConstants::EPD_HEIGHT, DisplayConstants::EPD_WIDTH) {}

void EpaperDisplay::begin()
{
    LOG_I(TAG, "Initializing E-Paper Display");

    // Initialiser pins
    pinMode(Pins::EINK_BUSY, INPUT);
    pinMode(Pins::EINK_RESET, OUTPUT);
    pinMode(Pins::EINK_DC, OUTPUT);
    pinMode(Pins::EINK_CS, OUTPUT);

    // Initialiser SPI
    SPI.begin(Pins::EINK_SCLK, -1, Pins::EINK_SDI, Pins::EINK_CS);
    SPI.beginTransaction(SPISettings(2000000, MSBFIRST, SPI_MODE0));

    // Nulstil og initialiser
    hardwareReset();
    softwareReset();
    initDisplay();

    // Clear buffers til start
    clearBuffers();

    setFont(); // Nulstil til standard font
    setTextColor(DisplayConstants::COLOR_BLACK);
    setTextSize(1); // Standard størrelse for at sikre det passer i header

    setRotation(0);

    // Juster tekstindstillinger
    setTextWrap(true);
    
    LOG_I(TAG, "E-Paper display initialized successfully");
    cp437(true); // Brug CP437 tegnsæt for specialtegn
}

void EpaperDisplay::hardwareReset()
{
    LOG_D(TAG, "Performing hardware reset");
    digitalWrite(Pins::EINK_RESET, HIGH);
    TimeUtils::delay_for(TimeConstants::EPAPER_RESET_HIGH_DELAY);
    digitalWrite(Pins::EINK_RESET, LOW);
    TimeUtils::delay_for(TimeConstants::EPAPER_RESET_LOW_DELAY);
    digitalWrite(Pins::EINK_RESET, HIGH);
    TimeUtils::delay_for(TimeConstants::EPAPER_RESET_HIGH_DELAY);
}

void EpaperDisplay::softwareReset()
{
    LOG_D(TAG, "Sending software reset command");
    sendCommand(DisplayConstants::CMD_SWRESET);
    waitUntilIdle();
}

void EpaperDisplay::initDisplay()
{
    LOG_D(TAG, "Initializing display with basic commands");

    // Konfigurerer display scanning
    sendCommand(DisplayConstants::CMD_DRIVER_OUTPUT);
    sendData((DisplayConstants::EPD_HEIGHT - 1) & 0xFF);        // Højde (lavere byte)
    sendData(((DisplayConstants::EPD_HEIGHT - 1) >> 8) & 0xFF); // Højde (højere byte)
    sendData(DisplayConstants::PARAM_DRIVER_CONFIG);            // Konfigurationsbits

    // Konfigurerer display kant
    sendCommand(DisplayConstants::CMD_BORDER_WAVEFORM);
    sendData(DisplayConstants::PARAM_NORMAL_BORDER); // Normal kantbølgeform
}

void EpaperDisplay::clearBuffers()
{
    for (int i = 0; i < DisplayConstants::EPD_WIDTH * DisplayConstants::EPD_HEIGHT / 8; i++)
    {
        blackBuffer[i] = 0xFF; // Alt hvidt
        redBuffer[i] = 0x00;   // Intet rødt
    }
}

void EpaperDisplay::updateDisplay()
{

    // Send sort/hvid buffer til display
    sendCommand(DisplayConstants::CMD_WRITE_RAM_BLACK);
    for (int i = 0; i < DisplayConstants::EPD_WIDTH * DisplayConstants::EPD_HEIGHT / 8; i++)
    {
        sendData(blackBuffer[i]);
    }

    // Send rød buffer til display
    sendCommand(DisplayConstants::CMD_WRITE_RAM_RED);
    for (int i = 0; i < DisplayConstants::EPD_WIDTH * DisplayConstants::EPD_HEIGHT / 8; i++)
    {
        sendData(redBuffer[i]);
    }

    // Opdater displayet
    sendCommand(DisplayConstants::CMD_DISPLAY_UPDATE);
    sendData(DisplayConstants::PARAM_UPDATE_FULL);
    sendCommand(DisplayConstants::CMD_MASTER_ACTIVATION);
    waitUntilIdle();
}

void EpaperDisplay::sendCommand(uint8_t command)
{
    digitalWrite(Pins::EINK_DC, LOW);
    digitalWrite(Pins::EINK_CS, LOW);
    SPI.transfer(command);
    digitalWrite(Pins::EINK_CS, HIGH);
}

void EpaperDisplay::sendData(uint8_t data)
{
    digitalWrite(Pins::EINK_DC, HIGH);
    digitalWrite(Pins::EINK_CS, LOW);
    SPI.transfer(data);
    digitalWrite(Pins::EINK_CS, HIGH);
}

void EpaperDisplay::waitUntilIdle()
{
    unsigned long start = millis();
    while (digitalRead(Pins::EINK_BUSY) == LOW)
    {
        TimeUtils::delay_for(TimeConstants::EPAPER_BUSY_POLL_DELAY);
        if (millis() - start > TimeUtils::to_ms(TimeConstants::EPAPER_BUSY_TIMEOUT))
        {
            LOG_E(TAG, "waitUntilIdle timeout after %d seconds", TimeUtils::to_seconds(TimeConstants::EPAPER_BUSY_TIMEOUT));
            break;
        }
    }
}

void EpaperDisplay::fullRefresh()
{
    sendCommand(DisplayConstants::CMD_DISPLAY_UPDATE);
    sendData(DisplayConstants::PARAM_UPDATE_FULL);
    sendCommand(DisplayConstants::CMD_MASTER_ACTIVATION);
    waitUntilIdle();
}

void EpaperDisplay::setPixel(int16_t x, int16_t y, uint16_t color)
{
    if (x < 0 || x >= DisplayConstants::EPD_WIDTH || y < 0 || y >= DisplayConstants::EPD_HEIGHT)
    {
        return;
    }
    uint16_t byte_index = (x / 8) + (y * (DisplayConstants::EPD_WIDTH / 8));
    uint8_t bit_pos = 7 - (x % 8);

    if (color == DisplayConstants::COLOR_WHITE)
    {
        blackBuffer[byte_index] |= (1 << bit_pos);
        redBuffer[byte_index] &= ~(1 << bit_pos);
    }
    else if (color == DisplayConstants::COLOR_BLACK)
    {
        blackBuffer[byte_index] &= ~(1 << bit_pos);
        redBuffer[byte_index] &= ~(1 << bit_pos);
    }
    else if (color == DisplayConstants::COLOR_RED)
    {
        blackBuffer[byte_index] |= (1 << bit_pos);
        redBuffer[byte_index] |= (1 << bit_pos);
    }
}

void EpaperDisplay::drawPixel(int16_t x, int16_t y, uint16_t color)
{
    if ((x < 0) || (y < 0) || (x >= width()) || (y >= height()))
    {
        return;
    }

    setPixel(y, x, color);
}

void EpaperDisplay::drawTestPattern()
{
    LOG_D(TAG, "Drawing test pattern");

    // Tegn sort firkant øverst til venstre
    for (int y = 10; y < 60; y++)
    {
        for (int x = 10; x < 60; x++)
        {
            setPixel(x, y, DisplayConstants::COLOR_BLACK);
        }
    }

    // Tegn rød firkant nederst til højre
    for (int y = DisplayConstants::EPD_HEIGHT - 60; y < DisplayConstants::EPD_HEIGHT - 10; y++)
    {
        for (int x = DisplayConstants::EPD_WIDTH - 60; x < DisplayConstants::EPD_WIDTH - 10; x++)
        {
            setPixel(x, y, DisplayConstants::COLOR_RED);
        }
    }
}