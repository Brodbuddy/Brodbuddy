#include "network/time_manager.h"
#include "config/time_utils.h"
#include "config/constants.h"

const char* TimeManager::TAG = "TimeManager";

TimeManager::TimeManager() : 
    _timeInitialized(false),
    _timeZone("CET-1CEST,M3.5.0,M10.5.0/3"),
    _ntpServer("pool.ntp.org"),
    _lastSyncMillis(0),
    _syncInterval(TimeUtils::to_ms(TimeConstants::NTP_SYNC_INTERVAL)) {
}

bool TimeManager::begin(const char* timeZone, const char* ntpServer) {
    _timeZone = timeZone;
    _ntpServer = ntpServer;
    
    setTimeZone(timeZone);
    
    return syncWithNTP();
}

void TimeManager::loop() {
    if (_timeInitialized && ((unsigned long)(millis() - _lastSyncMillis) > _syncInterval)) {
        LOG_I(TAG, "Performing periodic NTP sync");
        syncWithNTP();
    }
}

time_t TimeManager::getEpochTime() {
    if (!_timeInitialized) {
        LOG_W(TAG, "Time not initialized, returning estimated time");
        return TimeConstants::FALLBACK_EPOCH;
    }
    return time(nullptr);
}

String TimeManager::getISOTime() {
    char timeBuffer[25];
    time_t now = getEpochTime();
    struct tm timeinfo;
    
    gmtime_r(&now, &timeinfo);
    
    strftime(timeBuffer, sizeof(timeBuffer), "%Y-%m-%dT%H:%M:%SZ", &timeinfo);
    
    return String(timeBuffer);
}

String TimeManager::getLocalTimeString() {
    char timeBuffer[25];
    time_t now = getEpochTime();
    struct tm timeinfo;
    
    localtime_r(&now, &timeinfo);
    
    strftime(timeBuffer, sizeof(timeBuffer), "%Y-%m-%d %H:%M:%S", &timeinfo);
    
    return String(timeBuffer);
}

String TimeManager::getLocalTimeISO() {
    char timeBuffer[25];
    time_t now = getEpochTime();
    struct tm timeinfo;
    
    localtime_r(&now, &timeinfo);
    
    strftime(timeBuffer, sizeof(timeBuffer), "%Y-%m-%dT%H:%M:%S", &timeinfo);
    
    return String(timeBuffer);
}

void TimeManager::setTimeZone(const char* timeZone) {
    _timeZone = timeZone;
    
    setenv("TZ", timeZone, 1);
    tzset();
}

bool TimeManager::syncWithNTP() {
    LOG_I(TAG, "Synchronizing with NTP server: %s", _ntpServer);
    configTime(0, 0, _ntpServer);
    
    setenv("TZ", _timeZone.c_str(), 1);
    tzset();
    
    int retry = 0;
    const int maxRetries = 10;
    
    while (!isTimeValid() && retry < maxRetries) {
        LOG_D(TAG, "Waiting for NTP time... attempt %d/%d", retry+1, maxRetries);
        TimeUtils::delay_for(TimeConstants::NTP_SYNC_RETRY_DELAY);
        retry++;
    }
    
    if (isTimeValid()) {
        _timeInitialized = true;
        _lastSyncMillis = millis();
        LOG_I(TAG, "NTP sync successful. Local time: %s", getLocalTimeString().c_str());
        return true;
    }
    
    LOG_E(TAG, "NTP sync failed after %d attempts", maxRetries);
    return false;
}

bool TimeManager::isTimeValid() {
    time_t now = time(nullptr);
    return now > TimeConstants::MIN_VALID_EPOCH;
}

void TimeManager::adjustAfterSleep(unsigned long sleepTimeMs) {
    if (sleepTimeMs > TimeUtils::to_ms(TimeConstants::LONG_SLEEP_THRESHOLD)) {
        LOG_I(TAG, "Performing NTP sync after long sleep (%lu ms)", sleepTimeMs);
        syncWithNTP();
    } else {
        LOG_D(TAG, "Skipping NTP sync after short sleep (%lu ms)", sleepTimeMs);
    }
}