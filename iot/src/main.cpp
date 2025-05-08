#include <Arduino.h>
#include <GxEPD2_BW.h>
#include <GxEPD2_3C.h>
#include <Fonts/FreeMonoBold9pt7b.h>

// Function prototype - add this line
void addTextOnly();

// DISPLAY MODEL OPTIONS - Uncomment only ONE at a time
// Option 1: Standard 2.9" B/W model
// GxEPD2_BW<GxEPD2_290, GxEPD2_290::HEIGHT> display(GxEPD2_290(/*CS=*/15, /*DC=*/17, /*RST=*/21, /*BUSY=*/4));

// Option 2: 2.9" B/W BS model
GxEPD2_BW<GxEPD2_290_BS, GxEPD2_290_BS::HEIGHT> display(GxEPD2_290_BS(/*CS=*/15, /*DC=*/17, /*RST=*/21, /*BUSY=*/4));

// Option 3: 2.9" B/W/R Z13 model for 3-color displays
//GxEPD2_3C<GxEPD2_290_Z13, GxEPD2_290_Z13::HEIGHT> display(GxEPD2_290_Z13(/*CS=*/15, /*DC=*/17, /*RST=*/21, /*BUSY=*/4));

void setup() {
  Serial.begin(115200);
  delay(1000);
  Serial.println("Minimal E-Paper Text Test");
  
  // Only initialize the display, don't clear or reset it
  display.init(0, false, 2, false);
  display.setRotation(1); // Landscape orientation
  
  // Draw text without changing background
  addTextOnly();
  
  Serial.println("Text drawing complete");
}

void loop() {
  delay(60000); // Just wait
}

void addTextOnly() {
  // Prepare partial window for text
  int x = 10;
  int y = 30;
  int w = 280;
  int h = 100;
  
  // Set partial window for text area only
  display.setPartialWindow(x, y, w, h);
  
  // Start drawing session
  display.firstPage();
  do {
    // Set font and text properties
    display.setFont(&FreeMonoBold9pt7b);
    display.setTextColor(GxEPD_BLACK);
    
    // Draw text at specific positions - don't fill background!
    display.setCursor(x, y + 20);
    display.print("Borge Sourdough");
    
    display.setCursor(x, y + 50);
    display.print("Rise: 35.0%");
    
    display.setCursor(x, y + 80);
    display.print("Temp: 22.9C");
    
  } while (display.nextPage());
}