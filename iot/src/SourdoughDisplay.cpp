#include "SourdoughDisplay.h"

SourdoughDisplay::SourdoughDisplay() {}

void SourdoughDisplay::begin() {
    Serial.println("Initializing E-Paper Display...");

    // Initialiser pins
    pinMode(EINK_BUSY, INPUT);
    pinMode(EINK_RESET, OUTPUT);
    pinMode(EINK_DC, OUTPUT);
    pinMode(EINK_CS, OUTPUT);
    
    // Initialiser SPI
    SPI.begin(EINK_SCLK, EINK_MISO, EINK_MOSI, EINK_CS);
    SPI.beginTransaction(SPISettings(2000000, MSBFIRST, SPI_MODE0));

    // Nulstil og initialiser
    hardwareReset();
    softwareReset();
    initDisplay();

    // Clear buffers til start
    clearBuffers();
}

void SourdoughDisplay::hardwareReset() {
    Serial.println("Performing extended hardware reset...");
    digitalWrite(EINK_RESET, HIGH); delay(200);
    digitalWrite(EINK_RESET, LOW);  delay(500);
    digitalWrite(EINK_RESET, HIGH); delay(200);
}

void SourdoughDisplay::softwareReset() {
    Serial.println("Sending software reset command...");
    sendCommand(CMD_SWRESET);
    waitUntilIdle();
}

void SourdoughDisplay::initDisplay() {
    Serial.println("Initializing display with basic commands...");
    
    // Konfigurerer display scanning
    sendCommand(CMD_DRIVER_OUTPUT);
    sendData((EPD_HEIGHT - 1) & 0xFF);        // Højde (lavere byte)
    sendData(((EPD_HEIGHT - 1) >> 8) & 0xFF); // Højde (højere byte)
    sendData(PARAM_DRIVER_CONFIG);            // Konfigurationsbits
    
    // Konfigurerer display kant
    sendCommand(CMD_BORDER_WAVEFORM);
    sendData(PARAM_NORMAL_BORDER);            // Normal kantbølgeform
}


void SourdoughDisplay::clearBuffers() {
    Serial.println("Clearing display buffers...");
    for (int i = 0; i < EPD_WIDTH * EPD_HEIGHT / 8; i++) {
        blackBuffer[i] = 0xFF; // Alt hvidt
        redBuffer[i] = 0x00;   // Intet rødt
    }
}

void SourdoughDisplay::updateDisplay() {
    Serial.println("Sending buffer data to display RAM...");
    
    // Send sort/hvid buffer til display
    sendCommand(CMD_WRITE_RAM_BLACK);
    for (int i = 0; i < EPD_WIDTH * EPD_HEIGHT / 8; i++) {
        sendData(blackBuffer[i]);
    }
    
    // Send rød buffer til display
    sendCommand(CMD_WRITE_RAM_RED);
    for (int i = 0; i < EPD_WIDTH * EPD_HEIGHT / 8; i++) {
        sendData(redBuffer[i]);
    }
    
    // Opdater displayet
    sendCommand(CMD_DISPLAY_UPDATE);
    sendData(PARAM_UPDATE_FULL);
    sendCommand(CMD_MASTER_ACTIVATION);
    waitUntilIdle();
}

void SourdoughDisplay::sendCommand(uint8_t command) {
    digitalWrite(EINK_DC, LOW);
    digitalWrite(EINK_CS, LOW);
    SPI.transfer(command);
    digitalWrite(EINK_CS, HIGH);
}

void SourdoughDisplay::sendData(uint8_t data) {
    digitalWrite(EINK_DC, HIGH);
    digitalWrite(EINK_CS, LOW);
    SPI.transfer(data);
    digitalWrite(EINK_CS, HIGH);
}


void SourdoughDisplay::waitUntilIdle() {
    unsigned long start = millis();
    while(digitalRead(EINK_BUSY) == LOW) {
        delay(10);
        if (millis() - start > 5000) { // 5 sekunder timeout
            Serial.println("waitUntilIdle TIMEOUT!");
            break;
        }
    }
}

void SourdoughDisplay::fullRefresh() {
    sendCommand(CMD_DISPLAY_UPDATE);
    sendData(PARAM_UPDATE_FULL);   
    sendCommand(CMD_MASTER_ACTIVATION); 
    waitUntilIdle();
}

void SourdoughDisplay::setPixel(int x, int y, uint8_t color) {
    if (x < 0 || x >= EPD_WIDTH || y < 0 || y >= EPD_HEIGHT) {
        return;
    }
    uint16_t byte_index = (x / 8) + (y * (EPD_WIDTH / 8));
    uint8_t bit_pos = 7 - (x % 8);

    if (color == COLOR_WHITE) { 
        blackBuffer[byte_index] |= (1 << bit_pos); 
        redBuffer[byte_index] &= ~(1 << bit_pos);
    } else if (color == COLOR_BLACK) { 
        blackBuffer[byte_index] &= ~(1 << bit_pos);
        redBuffer[byte_index] &= ~(1 << bit_pos); 
    } else if (color == COLOR_RED) { 
        blackBuffer[byte_index] |= (1 << bit_pos); 
        redBuffer[byte_index] |= (1 << bit_pos);  
    }
}

void SourdoughDisplay::drawTestPattern() {
    Serial.println("Drawing test pattern into buffers...");
    
     // Tegner en sort firkant i øverste venstre kvadrant
    for (int y = 10; y < (EPD_HEIGHT / 4); y++) {
        for (int x = 10; x < (EPD_WIDTH / 2) - 10; x++) {
            setPixel(x, y, COLOR_BLACK);
        }
    }
    
    // Tegner en rød firkant i nederste højre kvadrant
    for (int y = (EPD_HEIGHT * 3 / 4); y < EPD_HEIGHT - 10; y++) {
        for (int x = (EPD_WIDTH / 2) + 10; x < EPD_WIDTH - 10; x++) {
            setPixel(x, y, COLOR_RED);
        }
    }
}