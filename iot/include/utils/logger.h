#ifndef LOGGER_H
#define LOGGER_H

#include <Arduino.h>

enum LogLevel {
    LOG_ERROR = 0,
    LOG_WARNING = 1,
    LOG_INFO = 2,
    LOG_DEBUG = 3
};

#define ANSI_COLOR_RED     "\x1b[31m"
#define ANSI_COLOR_YELLOW  "\x1b[33m"
#define ANSI_COLOR_GREEN   "\x1b[32m"
#define ANSI_COLOR_CYAN    "\x1b[36m"
#define ANSI_COLOR_RESET   "\x1b[0m"
#define ANSI_COLOR_BOLD    "\x1b[1m"

#ifndef LOG_LEVEL
    #define LOG_LEVEL LOG_INFO
#endif

class Logger {
private:
    static LogLevel _level;
    static bool _serialEnabled;
    static bool _colorsEnabled;
    static void log(LogLevel level, const char* levelStr, const char* color, const char* tag, const char* format, va_list args);

    public:
    static void begin(LogLevel level = LOG_INFO, bool enableColors = true);
    static void setLevel(LogLevel level) { _level = level; }
    static void enableColors(bool enable) { _colorsEnabled = enable; }

    static void error(const char* tag, const char* format, ...);
    static void warning(const char* tag, const char* format, ...);
    static void info(const char* tag, const char* format, ...);
    static void debug(const char* tag, const char* format, ...);
};

#if LOG_LEVEL >= LOG_ERROR
    #define LOG_E(tag, ...) Logger::error(tag, __VA_ARGS__)
#else
    #define LOG_E(tag, ...) do {} while(0)
#endif

#if LOG_LEVEL >= LOG_WARNING
    #define LOG_W(tag, ...) Logger::warning(tag, __VA_ARGS__)
#else
    #define LOG_W(tag, ...) do {} while(0)
#endif

#if LOG_LEVEL >= LOG_INFO
    #define LOG_I(tag, ...) Logger::info(tag, __VA_ARGS__)
#else
    #define LOG_I(tag, ...) do {} while(0)
#endif

#if LOG_LEVEL >= LOG_DEBUG
    #define LOG_D(tag, ...) Logger::debug(tag, __VA_ARGS__)
#else
    #define LOG_D(tag, ...) do {} while(0)
#endif

#endif