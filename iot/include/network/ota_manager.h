#pragma once

#include <Arduino.h>
#include <Update.h>
#include "esp_ota_ops.h"
#include "logging/logger.h"
#include "config/time_utils.h"
#include <functional>

class OtaManager {
public:
    enum class OtaStatus {
        IDLE = 0,
        DOWNLOADING = 1,
        APPLYING = 2,
        REBOOTING = 3,
        ERROR = 4,
        COMPLETE = 5
    };

    struct OtaInfo {
        String version;
        uint32_t size;
        uint32_t crc32;
    };

    using BatteryCheckCallback = std::function<bool()>;
    using StatusCallback = std::function<void(const String& status, uint8_t progress)>;

    OtaManager();
    
    bool begin();
    bool startUpdate(const OtaInfo& info);
    bool processChunk(const uint8_t* data, size_t length, uint32_t chunkIndex, uint32_t chunkSize);
    void abort(const String& reason);
    
    void setBatteryCheckCallback(BatteryCheckCallback callback) { batteryCheck = callback; }
    void setStatusCallback(StatusCallback callback) { statusCallback = callback; }
    
    bool handleOtaMessage(const String& topic, const uint8_t* payload, unsigned int length, 
                         const String& otaStartTopic, const String& otaChunkTopic);
    
    OtaStatus getStatus() const { return status; }
    uint8_t getProgress() const;
    bool isInProgress() const { return inProgress; }
    uint32_t getReceivedBytes() const { return receivedBytes; }
    uint32_t getTotalBytes() const { return totalBytes; }
    std::chrono::steady_clock::time_point getLastChunkTime() const { return lastChunkTime; }
    
private:
    static const char* TAG;
    
    OtaStatus status;
    bool inProgress;
    esp_ota_handle_t otaHandle;
    const esp_partition_t* updatePartition;
    
    uint32_t totalBytes;
    uint32_t receivedBytes;
    uint32_t expectedCrc32;
    uint32_t calculatedCrc32;
    std::chrono::steady_clock::time_point lastChunkTime;
    
    BatteryCheckCallback batteryCheck;
    StatusCallback statusCallback;
    
    void completeUpdate();
    uint32_t updateCrc32(uint32_t crc, const uint8_t* data, size_t length);
};