#include "utils/logger.h"

LogLevel Logger::_level = LOG_INFO;
bool Logger::_serialEnabled = true;

void Logger::begin(LogLevel level) {
    _level = level;
}

void Logger::log(LogLevel level, const char* tag, const char* format, va_list args) {
    if (level > _level || !_serialEnabled) return;

    const char* levelStr[] = {"ERROR", "WARN", "INFO", "DEBUG"};

    Serial.printf("[%lu][%s][%s] ", millis(), levelStr[level], tag);
    char buffer[256];
    vsnprintf(buffer, sizeof(buffer), format, args);
    Serial.println(buffer);
}

void Logger::error(const char* tag, const char* format, ...) {
    va_list args;
    va_start(args, format);
    log(LOG_ERROR, tag, format, args);
    va_end(args);
}

void Logger::warning(const char* tag, const char* format, ...) {
    va_list args;
    va_start(args, format);
    log(LOG_WARNING, tag, format, args);
    va_end(args);
}

void Logger::info(const char* tag, const char* format, ...) {
    va_list args;
    va_start(args, format);
    log(LOG_INFO, tag, format, args);
    va_end(args);
}

void Logger::debug(const char* tag, const char* format, ...) {
    va_list args;
    va_start(args, format);
    log(LOG_DEBUG, tag, format, args);
    va_end(args);
}