#include "utils/logger.h"

LogLevel Logger::_level = LOG_INFO;
bool Logger::_serialEnabled = true;
bool Logger::_colorsEnabled = true;

void Logger::begin(LogLevel level, bool enableColors) {
    _level = level;
    _colorsEnabled = enableColors;
    _serialEnabled = true;
    
    if (_serialEnabled) {
        Serial.println();
        if (_colorsEnabled) {
            Serial.printf("%s%s[LOGGER]%s Logger initialized with colors\n", 
                         ANSI_COLOR_BOLD, ANSI_COLOR_GREEN, ANSI_COLOR_RESET);
        } else {
            Serial.println("[LOGGER] Logger initialized");
        }
    }
}

void Logger::log(LogLevel level, const char* levelStr, const char* color, const char* tag, const char* format, va_list args) {
    if (!_serialEnabled || level > _level) {
        return;
    }

    unsigned long timestamp = millis();

    if (_colorsEnabled) {
        Serial.printf("%s[%lu]%s%s[%s]%s%s[%s]%s ", 
                     ANSI_COLOR_CYAN, timestamp, ANSI_COLOR_RESET,
                     color, levelStr, ANSI_COLOR_RESET,
                     ANSI_COLOR_BOLD, tag, ANSI_COLOR_RESET);
    } else {
        Serial.printf("[%lu][%s][%s] ", timestamp, levelStr, tag);
    }
    
    char buffer[256];
    vsnprintf(buffer, sizeof(buffer), format, args);
    Serial.println(buffer);
}

void Logger::error(const char* tag, const char* format, ...) {
    va_list args;
    va_start(args, format);
    log(LOG_ERROR, "E", ANSI_COLOR_RED, tag, format, args);
    va_end(args);
}

void Logger::warning(const char* tag, const char* format, ...) {
    va_list args;
    va_start(args, format);
    log(LOG_WARNING, "W", ANSI_COLOR_YELLOW, tag, format, args);
    va_end(args);
}

void Logger::info(const char* tag, const char* format, ...) {
    va_list args;
    va_start(args, format);
    log(LOG_INFO, "I", ANSI_COLOR_GREEN, tag, format, args);
    va_end(args);
}

void Logger::debug(const char* tag, const char* format, ...) {
    va_list args;
    va_start(args, format);
    log(LOG_DEBUG, "D", ANSI_COLOR_CYAN, tag, format, args);
    va_end(args);
}