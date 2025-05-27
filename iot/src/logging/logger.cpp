#include "logging/logger.h"

LogLevel Logger::_level = LOG_INFO;
bool Logger::_serialEnabled = true;
bool Logger::_colorsEnabled = true;
bool Logger::_otaEnabled = false;

void Logger::begin(LogLevel level, bool enableColors, bool enableOta) {
    _level = level;
    _colorsEnabled = enableColors;
    _otaEnabled = enableOta;
    _serialEnabled = true;

    if (_serialEnabled) {
        Serial.println();
        if (_colorsEnabled) {
            Serial.printf("%s%s[LOGGER]%s Logger initialized with colors%s\n", ANSI_COLOR_BOLD, ANSI_COLOR_GREEN,
                          ANSI_COLOR_RESET, _otaEnabled ? " and OTA logging" : "");
        } else {
            Serial.printf("[LOGGER] Logger initialized%s\n", _otaEnabled ? " with OTA logging" : "");
        }
    }
}

void Logger::log(LogLevel level, const char* levelStr, const char* color, const char* tag, const char* format,
                 va_list args) {
    if (!_serialEnabled || level > _level) {
        return;
    }

    unsigned long timestamp = millis();

    if (_colorsEnabled) {
        Serial.printf("%s[%lu]%s%s[%s]%s%s[%s]%s ", ANSI_COLOR_CYAN, timestamp, ANSI_COLOR_RESET, color, levelStr,
                      ANSI_COLOR_RESET, ANSI_COLOR_BOLD, tag, ANSI_COLOR_RESET);
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

void Logger::ota(const char* tag, const char* format, ...) {
    if (!_serialEnabled || !_otaEnabled) {
        return;
    }

    unsigned long timestamp = millis();
    va_list args;
    va_start(args, format);

    if (_colorsEnabled) {
        Serial.printf("%s[%lu]%s%s[O]%s%s[%s]%s ", ANSI_COLOR_CYAN, timestamp, ANSI_COLOR_RESET, ANSI_COLOR_MAGENTA, 
                      ANSI_COLOR_RESET, ANSI_COLOR_BOLD, tag, ANSI_COLOR_RESET);
    } else {
        Serial.printf("[%lu][O][%s] ", timestamp, tag);
    }

    char buffer[256];
    vsnprintf(buffer, sizeof(buffer), format, args);
    Serial.println(buffer);
    va_end(args);
}