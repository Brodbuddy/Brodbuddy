#ifndef FONTS_H
#define FONTS_H

#include <Arduino.h>

// Standard 5x7 font
extern const unsigned char Font5x7[] PROGMEM;

// Font dimensions and spacing
#define FONT_WIDTH 5
#define FONT_HEIGHT 7
#define FONT_SPACING 1

// First character in font is space (ASCII 32)
#define FONT_FIRST_CHAR 32
// Number of characters in the font
#define FONT_CHAR_COUNT 96

// Danish characters indices (we'll add these at the end of the standard font)
#define CHAR_AE 128  // æ
#define CHAR_OE 129  // ø
#define CHAR_AA 130  // å

#endif // FONTS_H