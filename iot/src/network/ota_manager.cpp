#include "network/ota_manager.h"
#include <ArduinoJson.h>
#include "network/mqtt_protocol.h"
#include "config/constants.h"

const char* OtaManager::TAG = "OtaManager";

OtaManager::OtaManager() 
    : status(OtaStatus::IDLE)
    , inProgress(false)
    , otaHandle(0)
    , updatePartition(nullptr)
    , totalBytes(0)
    , receivedBytes(0)
    , expectedCrc32(0)
    , calculatedCrc32(0)
    , batteryCheck(nullptr)
    , statusCallback(nullptr) {
}

bool OtaManager::begin() {
    LOG_I(TAG, "OTA Manager initialized");
    return true;
}

bool OtaManager::startUpdate(const OtaInfo& info) {
    if (inProgress) {
        LOG_W(TAG, "Update already in progress");
        return false;
    }
    
    if (batteryCheck && !batteryCheck()) {
        LOG_E(TAG, "Battery check failed - insufficient power for OTA");
        return false;
    }
    
    LOG_I(TAG, "Starting OTA update - Version: %s, Size: %d bytes, CRC32: 0x%08X", 
          info.version.c_str(), info.size, info.crc32);
    
    updatePartition = esp_ota_get_next_update_partition(nullptr);
    if (!updatePartition) {
        LOG_E(TAG, "No OTA partition available");
        return false;
    }
    
    LOG_I(TAG, "Target partition: %s, size: %d bytes", 
          updatePartition->label, updatePartition->size);
    
    if (info.size > updatePartition->size) {
        LOG_E(TAG, "Firmware too large: %d > %d", info.size, updatePartition->size);
        return false;
    }
    
    esp_err_t err = esp_ota_begin(updatePartition, OTA_WITH_SEQUENTIAL_WRITES, &otaHandle);
    if (err != ESP_OK) {
        LOG_E(TAG, "Failed to begin OTA: %s", esp_err_to_name(err));
        return false;
    }
    
    status = OtaStatus::DOWNLOADING;
    inProgress = true;
    totalBytes = info.size;
    expectedCrc32 = info.crc32;
    receivedBytes = 0;
    calculatedCrc32 = 0;
    lastChunkTime = std::chrono::steady_clock::now();
    
    LOG_I(TAG, "OTA update started successfully");
    return true;
}

bool OtaManager::processChunk(const uint8_t* data, size_t length, uint32_t chunkIndex, uint32_t chunkSize) {
    if (!inProgress || status != OtaStatus::DOWNLOADING) {
        LOG_E(TAG, "Not ready to process chunk - inProgress: %d, status: %d", 
              inProgress, static_cast<int>(status));
        return false;
    }
    
    auto now = std::chrono::steady_clock::now();
    auto timeSinceLastChunk = std::chrono::duration_cast<std::chrono::milliseconds>(now - lastChunkTime);
    
    if (timeSinceLastChunk > TimeConstants::OTA_CHUNK_TIMEOUT) {
        abort("Chunk timeout");
        return false;
    }
    
    if (length != chunkSize) {
        abort("Chunk size mismatch");
        return false;
    }
    
    calculatedCrc32 = updateCrc32(calculatedCrc32, data, length);
    
    esp_err_t err = esp_ota_write(otaHandle, data, length);
    if (err != ESP_OK) {
        LOG_E(TAG, "esp_ota_write failed: %s (0x%x)", esp_err_to_name(err), err);
        abort("Write failed: " + String(esp_err_to_name(err)));
        return false;
    }
    
    receivedBytes += length;
    lastChunkTime = now;
    
    if (chunkIndex % OtaConstants::CHUNK_LOG_INTERVAL == 0 || receivedBytes >= totalBytes) {
        LOG_I(TAG, "OTA Progress: %d/%d bytes (%d%%)", 
              receivedBytes, totalBytes, getProgress());
        
        if (statusCallback) {
            statusCallback(MqttProtocol::OtaFields::StatusValues::DOWNLOADING, getProgress());
        }
    }
    
    if (receivedBytes >= totalBytes) {
        LOG_I(TAG, "All bytes received! Starting completion process...");
        completeUpdate();
    }
    
    return true;
}

void OtaManager::completeUpdate() {
    status = OtaStatus::APPLYING;
    
    if (statusCallback) {
        statusCallback(MqttProtocol::OtaFields::StatusValues::APPLYING, 100);
    }
    
    LOG_I(TAG, "Download complete, verifying CRC32...");
    
    if (calculatedCrc32 != expectedCrc32) {
        abort("CRC32 mismatch: expected 0x" + String(expectedCrc32, HEX) + 
              ", got 0x" + String(calculatedCrc32, HEX));
        return;
    }
    
    LOG_I(TAG, "CRC32 verified, finalizing update...");
    
    esp_err_t err = esp_ota_end(otaHandle);
    if (err != ESP_OK) {
        abort("Failed to finalize: " + String(esp_err_to_name(err)));
        return;
    }
    
    err = esp_ota_set_boot_partition(updatePartition);
    if (err != ESP_OK) {
        abort("Failed to set boot partition: " + String(esp_err_to_name(err)));
        return;
    }
    
    status = OtaStatus::COMPLETE;
    inProgress = false;
    
    if (statusCallback) {
        statusCallback(MqttProtocol::OtaFields::StatusValues::COMPLETE, 100);
    }
    
    LOG_I(TAG, "OTA update successful, rebooting in 3 seconds...");
    
    status = OtaStatus::REBOOTING;
    TimeUtils::delay_for(TimeConstants::OTA_REBOOT_DELAY);
    ESP.restart();
}

void OtaManager::abort(const String& reason) {
    LOG_E(TAG, "Aborting OTA: %s", reason.c_str());
    
    if (otaHandle != 0) {
        esp_ota_abort(otaHandle);
        otaHandle = 0;
    }
    
    status = OtaStatus::ERROR;
    inProgress = false;
    updatePartition = nullptr;
    totalBytes = 0;
    receivedBytes = 0;
}

uint8_t OtaManager::getProgress() const {
    if (totalBytes == 0) return 0;
    return (receivedBytes * 100) / totalBytes;
}

uint32_t OtaManager::updateCrc32(uint32_t crc, const uint8_t* data, size_t length) {
    static const uint32_t polynomial = 0xEDB88320;
    crc = ~crc;
    
    for (size_t i = 0; i < length; i++) {
        crc ^= data[i];
        for (int j = 0; j < 8; j++) {
            crc = (crc >> 1) ^ ((crc & 1) * polynomial);
        }
    }
    
    return ~crc;
}

bool OtaManager::handleOtaMessage(const String& topic, const uint8_t* payload, unsigned int length, 
                                  const String& otaStartTopic, const String& otaChunkTopic) {
    if (topic == otaStartTopic) {
        DynamicJsonDocument doc(256);
        DeserializationError error = deserializeJson(doc, payload, length);
        
        if (error) {
            LOG_E(TAG, "Failed to parse OTA start message: %s", error.c_str());
            return false;
        }
        
        OtaInfo info;
        info.version = doc[MqttProtocol::OtaFields::VERSION].as<String>();
        info.size = doc[MqttProtocol::OtaFields::SIZE];
        info.crc32 = doc[MqttProtocol::OtaFields::CRC32];
        
        if (startUpdate(info)) {
            if (statusCallback) {
                statusCallback(MqttProtocol::OtaFields::StatusValues::STARTED, 0);
            }
            return true;
        } else {
            if (statusCallback) {
                statusCallback(MqttProtocol::OtaFields::StatusValues::ERROR, 0);
            }
            return false;
        }
    }
    else if (topic == otaChunkTopic) {
        if (length < OtaConstants::CHUNK_HEADER_SIZE) {
            LOG_E(TAG, "Invalid OTA chunk - too small");
            return false;
        }
        
        uint32_t chunkIndex, chunkSize;
        memcpy(&chunkIndex, payload, 4);
        memcpy(&chunkSize, payload + 4, 4);
        
        const uint8_t* data = payload + OtaConstants::CHUNK_HEADER_SIZE;
        size_t dataLength = length - OtaConstants::CHUNK_HEADER_SIZE;
        
        if (chunkIndex % OtaConstants::CHUNK_LOG_INTERVAL == 0) {
            LOG_I(TAG, "Processing OTA chunk %d/%d", chunkIndex, OtaConstants::ESTIMATED_TOTAL_CHUNKS);
        }
        
        bool result = processChunk(data, dataLength, chunkIndex, chunkSize);
        
        if (!result) {
            if (statusCallback) {
                statusCallback(MqttProtocol::OtaFields::StatusValues::ERROR, getProgress());
            }
        }
        
        return result;
    }
    
    return false;
}