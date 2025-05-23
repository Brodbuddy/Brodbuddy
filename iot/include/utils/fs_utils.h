#ifndef FS_UTILS_H
#define FS_UTILS_H

#include <Arduino.h>
#include <LittleFS.h>
#include "utils/logger.h"

inline bool waitForFilesystemIdle(unsigned long timeout = 1000) {
    static const char* TAG = "FSUtils";
    unsigned long start = millis();
    
    while (LittleFS.begin()) {
        if ((unsigned long)(millis() - start) > timeout) {
            LOG_W(TAG, "Filesystem busy timeout after %lu ms", timeout);
            return false;
        }
        delay(10);
    }
    
    LOG_D(TAG, "Filesystem idle confirmed");
    return true;
}

#endif