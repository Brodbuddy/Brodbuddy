#ifndef LOGGER_H
#define LOGGER_H

#include <Arduino.h>

enum LogLevel {
    LOG_ERROR = 0,
    LOG_WARNING = 1,
    LOG_INFO = 2,
    LOG_DEBUG = 3
};

class Logger {
private:
    static LogLevel _level;
    static bool _serialEnabled;
    static void log(LogLevel level, const char* tag, const char* format, va_list args);
public:
    static void begin(LogLevel level = LOG_INFO);
    static void setLevel(LogLevel level) { _level = level; }

    static void error(const char* tag, const char* format, ...);
    static void warning(const char* tag, const char* format, ...);
    static void info(const char* tag, const char* format, ...);
    static void debug(const char* tag, const char* format, ...);
};

#define LOG_E(tag, ...) Logger::error(tag, __VA_ARGS__)
#define LOG_W(tag, ...) Logger::warning(tag, __VA_ARGS__)
#define LOG_I(tag, ...) Logger::info(tag, __VA_ARGS__)
#define LOG_D(tag, ...) Logger::debug(tag, __VA_ARGS__)

#endif