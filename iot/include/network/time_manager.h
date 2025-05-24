#ifndef TIME_MANAGER_H
#define TIME_MANAGER_H

#include <Arduino.h>
#include <time.h>
#include "logging/logger.h"

class TimeManager {
private:
    bool _timeInitialized;
    String _timeZone;
    const char* _ntpServer;
    unsigned long _lastSyncMillis;
    unsigned long _syncInterval;
    
    static const char* TAG;

public:
    TimeManager();
    
    bool begin(const char* timeZone = "CET-1CEST,M3.5.0,M10.5.0/3", 
               const char* ntpServer = "pool.ntp.org");
    
    void loop();
    
    time_t getEpochTime();
    String getISOTime();
    String getLocalTimeString();
    String getLocalTimeISO();
    
    void setTimeZone(const char* timeZone);
    
    bool syncWithNTP();
    
    bool isTimeValid();
    
    void adjustAfterSleep(unsigned long sleepTimeMs);
};

#endif