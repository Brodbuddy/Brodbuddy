#include <Arduino.h>
#include <ArduinoJson.h>
#include <WiFi.h>
#include <Update.h>
#include "esp_ota_ops.h"

#include "config/constants.h"
#include "config/settings.h"
#include "config/time_utils.h"
#include "app/data_types.h"
#include "app/state_machine.h"
#include "app/epaper_monitor.h"
#include "hardware/button_manager.h"
#include "hardware/epaper_display.h"
#include "hardware/sensor_manager.h"
#include "network/wifi_manager.h"
#include "network/mqtt_manager.h"
#include "network/mqtt_topics.h"
#include "network/time_manager.h"
#include "network/mqtt_protocol.h"
#include "network/ota_manager.h"
#include "logging/logger.h"

static const char* TAG = "Main";

WifiManager wifiManager;
MqttManager mqttManager;
MqttTopics* mqttTopics = nullptr;
TimeManager timeManager;
StateMachine stateMachine;
SensorManager sensorManager;
ButtonManager buttonManager;
Settings settings;
EpaperDisplay display;
EpaperMonitor monitor(display);
SourdoughData historicalData = {};
OtaManager otaManager;

unsigned long lastStateCheck = 0;
bool needsOtaValidation = false;

void handleButtonEvents();
void handleCurrentState();
void handleStateBoot();
void handleStateConnectingWifi();
void handleStateConnectingMqtt();
void handleStateSensing();
void handleStateUpdatingDisplay();
void handleStatePublishingData();
void handleStateSleep();
void handleStateError();
void handleStateOtaUpdate();
void handleDiagnosticsRequest(char* topic, byte* payload, unsigned int length);
void setupDiagnosticsHandler();
void handleOtaMessage(char* topic, byte* payload, unsigned int length);
void setupOtaHandler();
void validateBootAfterOta();

void validateBootAfterOta() {
    const esp_partition_t* runningPartition = esp_ota_get_running_partition();
    esp_ota_img_states_t otaState;
    
    if (esp_ota_get_state_partition(runningPartition, &otaState) == ESP_OK) {
        if (otaState == ESP_OTA_IMG_PENDING_VERIFY) {
            LOG_I(TAG, "First boot after OTA update, validation pending...");
            LOG_I(TAG, "Running from partition: %s", runningPartition->label);
            needsOtaValidation = true;
        } else {
            LOG_D(TAG, "Normal boot from partition: %s", runningPartition->label);
        }
    }
}

void setup() {
    Serial.begin(115200);
    Logger::begin(LOG_DEBUG, true);

    LOG_I(TAG, "--- Sourdough analyzer startup monitoring ---");
    
    validateBootAfterOta();

    if (!settings.begin()) {
        LOG_E(TAG, "Failed to initialize settings");
        stateMachine.transitionTo(STATE_ERROR);
        return;
    }

    display.begin();
    
    historicalData.dataCount = 0;
    historicalData.oldestIndex = 0;
    historicalData.bufferFull = false;
    historicalData.outTemp = 21.0;
    historicalData.outHumidity = 44;
    historicalData.batteryLevel = 100;

    if (!sensorManager.begin()) {
        LOG_E(TAG, "Failed to initialize sensors");
    }

    sensorManager.setCalibration(settings.getTempOffset(), settings.getHumOffset());
    sensorManager.setReadInterval(TimeUtils::to_ms(std::chrono::seconds(settings.getSensorInterval())));
    LOG_I(TAG, "Sensor interval configured: %d seconds", settings.getSensorInterval());

    buttonManager.begin();
    if (buttonManager.isStartupResetPressed()) {
        LOG_I(TAG, "Reset button pressed during startup - clearing WiFi settings");
        wifiManager.resetSettings();
        TimeUtils::delay_for(TimeConstants::WIFI_RESTART_DELAY);
        ESP.restart();
    }

    wifiManager.begin();

    stateMachine.transitionTo(STATE_CONNECTING_WIFI);
}

void loop() {
    unsigned long currentMillis = millis();

    wifiManager.loop();
    buttonManager.loop();
    mqttManager.loop();
    timeManager.loop();

    if (currentMillis - lastStateCheck >= TimeUtils::to_ms(TimeConstants::STATE_CHECK_INTERVAL)) {
        lastStateCheck = currentMillis;

        handleButtonEvents();
        handleCurrentState();
    }
}

void handleButtonEvents() {
    if (buttonManager.isResetRequested()) {
        LOG_W(TAG, "WiFi reset requested");
        wifiManager.resetSettings();
        buttonManager.clearResetRequest();
        stateMachine.transitionTo(STATE_ERROR);
    }

    if (buttonManager.isPortalRequested()) {
        LOG_I(TAG, "Manual portal requested");
        buttonManager.clearPortalRequest();
        wifiManager.startCaptivePortal();
    }
}

void handleCurrentState() {
    switch (stateMachine.getCurrentState()) {
        case STATE_BOOT:
            handleStateBoot();
            break;
        case STATE_CONNECTING_WIFI:
            handleStateConnectingWifi();
            break;
        case STATE_CONNECTING_MQTT:
            handleStateConnectingMqtt();
            break;
        case STATE_SENSING:
            handleStateSensing();
            break;
        case STATE_UPDATING_DISPLAY:
            handleStateUpdatingDisplay();
            break;
        case STATE_PUBLISHING_DATA:
            handleStatePublishingData();
            break;
        case STATE_SLEEP:
            handleStateSleep();
            break;
        case STATE_OTA_UPDATE:
            handleStateOtaUpdate();
            break;
        case STATE_ERROR:
            handleStateError();
            break;
    }
}

void handleStateBoot() {
    stateMachine.transitionTo(STATE_CONNECTING_WIFI);
}

void handleStateConnectingWifi() {
    if (wifiManager.hasError()) {
        LOG_E(TAG, "WiFi manager error detected");
        stateMachine.transitionTo(STATE_ERROR);
    } else if (wifiManager.getStatus() == WIFI_CONNECTED) {
        TimeUtils::delay_for(TimeConstants::WIFI_STABILIZATION_DELAY);
        LOG_I(TAG, "WiFi connected");
        
        if (!timeManager.isTimeValid()) {
            LOG_I(TAG, "Synchronizing time with NTP server");
            if (timeManager.begin()) {
                LOG_I(TAG, "Time synchronized: %s", timeManager.getLocalTimeString().c_str());
            } else {
                LOG_W(TAG, "Time sync failed, continuing without accurate time");
            }
        }
        
        stateMachine.transitionTo(STATE_CONNECTING_MQTT);
    }
}

void handleStateConnectingMqtt() {
    if (!mqttManager.isConnected()) {
        String server = settings.getMqttServer();
        int port = settings.getMqttPort();
        String user = settings.getMqttUser();
        String password = settings.getMqttPassword();
        String analyzerId = settings.getAnalyzerId();

        LOG_D(TAG, "Connecting to MQTT - Server: %s, Port: %d, User: %s, Device: %s", server.c_str(), port,
              user.c_str(), analyzerId.c_str());
              
        if (mqttTopics == nullptr) {
            mqttTopics = new MqttTopics(analyzerId);
            mqttManager.setTopics(mqttTopics);
            LOG_I(TAG, "MQTT topics initialized for device: %s", analyzerId.c_str());
        }

        if (mqttManager.begin(server.c_str(), port, user.c_str(), password.c_str(), analyzerId.c_str())) {
            LOG_I(TAG, "MQTT connection established");
            
            if (needsOtaValidation) {
                LOG_I(TAG, "Marking OTA update as valid");
                esp_ota_mark_app_valid_cancel_rollback();
                needsOtaValidation = false;
            }
            
            setupDiagnosticsHandler();
            setupOtaHandler();
            stateMachine.transitionTo(STATE_SENSING);
        } else {
            LOG_E(TAG, "MQTT connection failed, retrying in 5 seconds");
            TimeUtils::delay_for(TimeConstants::MQTT_RETRY_DELAY);
        }
    } else {
        stateMachine.transitionTo(STATE_SENSING);
    }
}

void handleStateSensing() {
    static unsigned long lastSensingTime = 0;
    unsigned long currentTime = millis();
    
    if (lastSensingTime > 0) {
        unsigned long timeSinceLastSensing = currentTime - lastSensingTime;
        LOG_D(TAG, "New sensing cycle started - time since last: %lums (%.1fs), current_time=%s", 
              timeSinceLastSensing, timeSinceLastSensing / 1000.0, timeManager.getLocalTimeString().c_str());
    } else {
        LOG_D(TAG, "First sensing cycle started - current_time=%s", timeManager.getLocalTimeString().c_str());
    }
    lastSensingTime = currentTime;
    
    if (sensorManager.shouldRead()) {
        LOG_I(TAG, "Collecting sensor samples...");
        if (sensorManager.collectMultipleSamples()) {
            LOG_I(TAG, "Sensor reading complete");
            
            SensorData sensorData = sensorManager.getCurrentData();
            historicalData.inTemp = sensorData.inTemp;
            historicalData.inHumidity = (int)sensorData.inHumidity;
            historicalData.currentGrowth = (int)sensorData.currentRisePercent;
            
            unsigned long timestamp = timeManager.getEpochTime();
            monitor.addDataPoint(historicalData, (int)sensorData.currentRisePercent, timestamp);
            LOG_I(TAG, "Added data point: growth=%d%%, timestamp=%lu", (int)sensorData.currentRisePercent, timestamp);
            
            stateMachine.transitionTo(STATE_UPDATING_DISPLAY);
        }
    }
}

void handleStateUpdatingDisplay() {
    monitor.updateDisplay(historicalData);
    LOG_I(TAG, "Display updated");
    stateMachine.transitionTo(STATE_PUBLISHING_DATA);
}

void handleStatePublishingData() {
    if (!mqttManager.isConnected()) {
        stateMachine.transitionTo(STATE_CONNECTING_MQTT);
    } else {
        DynamicJsonDocument doc(512);
        SensorData data = sensorManager.getCurrentData();
        
        doc[MqttProtocol::TelemetryFields::EPOCH_TIME] = timeManager.getEpochTime();
        doc[MqttProtocol::TelemetryFields::TIMESTAMP] = timeManager.getISOTime();
        doc[MqttProtocol::TelemetryFields::LOCAL_TIME] = timeManager.getLocalTimeISO();
        doc[MqttProtocol::TelemetryFields::TEMPERATURE] = data.inTemp;
        doc[MqttProtocol::TelemetryFields::HUMIDITY] = data.inHumidity;
        doc[MqttProtocol::TelemetryFields::RISE] = data.currentRisePercent;

        if (mqttTopics && mqttManager.publish(mqttTopics->getTelemetryTopic().c_str(), doc)) {
            LOG_I(TAG, "Data published to %s", mqttTopics->getTelemetryTopic().c_str());
        } else {
            LOG_E(TAG, "Failed to publish data");
        }
        
        if (otaManager.isInProgress()) {
            LOG_I(TAG, "OTA in progress, staying awake");
            stateMachine.transitionTo(STATE_OTA_UPDATE);
        } else {
            stateMachine.transitionTo(STATE_SLEEP);
        }
    }
}

void handleStateSleep() {
    if (otaManager.isInProgress()) {
        LOG_W(TAG, "OTA in progress, preventing sleep");
        stateMachine.transitionTo(STATE_OTA_UPDATE);
        return;
    }
    
    if (settings.getLowPowerMode()) {
        WiFi.disconnect(true);
        WiFi.mode(WIFI_OFF);

        auto sleepDuration = std::chrono::seconds(settings.getSensorInterval());
        TimeUtils::enable_sleep_timer(sleepDuration);
        esp_light_sleep_start();

        timeManager.adjustAfterSleep(TimeUtils::to_ms(sleepDuration));

        if (!settings.begin()) {
            LOG_E(TAG, "Failed to re-initialize settings after sleep");
            stateMachine.transitionTo(STATE_ERROR);
            return;
        }

        WiFi.mode(WIFI_STA);
        WiFi.reconnect();
        TimeUtils::delay_for(std::chrono::seconds(1));

        stateMachine.transitionTo(STATE_CONNECTING_WIFI);
    } else {
        if (stateMachine.shouldTransition(TimeUtils::to_ms(std::chrono::seconds(settings.getSensorInterval())))) {
            unsigned long timeInState = stateMachine.timeInCurrentState();
            LOG_D(TAG, "State cycle timer: interval=%ds, time_in_state=%lums, current_time=%s", 
                  settings.getSensorInterval(), timeInState, timeManager.getLocalTimeString().c_str());
            stateMachine.transitionTo(STATE_SENSING);
        }
    }
}

void handleStateError() {
    LOG_E(TAG, "System in error state");
    TimeUtils::delay_for(TimeConstants::ERROR_STATE_DELAY);
    ESP.restart();
}

void setupDiagnosticsHandler() {
    if (!mqttTopics) {
        LOG_E(TAG, "Cannot setup diagnostics - topics not initialized");
        return;
    }
    
    mqttManager.setCallback(handleDiagnosticsRequest);
    
    if (mqttManager.subscribe(mqttTopics->getDiagnosticsRequestTopic().c_str())) {
        LOG_I(TAG, "Diagnostics handler registered on topic: %s", 
              mqttTopics->getDiagnosticsRequestTopic().c_str());
    }
}

void handleDiagnosticsRequest(char* topic, byte* payload, unsigned int length) {
    LOG_I(TAG, "Received diagnostics request");
    
    DynamicJsonDocument responseDoc(512);
    
    responseDoc[MqttProtocol::DiagnosticsFields::ANALYZER_ID] = settings.getAnalyzerId();
    responseDoc[MqttProtocol::DiagnosticsFields::EPOCH_TIME] = timeManager.getEpochTime();
    responseDoc[MqttProtocol::DiagnosticsFields::TIMESTAMP] = timeManager.getISOTime();
    responseDoc[MqttProtocol::DiagnosticsFields::LOCAL_TIME] = timeManager.getLocalTimeISO();
    responseDoc[MqttProtocol::DiagnosticsFields::UPTIME] = millis();
    responseDoc[MqttProtocol::DiagnosticsFields::FREE_HEAP] = ESP.getFreeHeap();
    responseDoc[MqttProtocol::DiagnosticsFields::STATE] = stateMachine.getStateName();
    
    responseDoc[MqttProtocol::DiagnosticsFields::WIFI][MqttProtocol::DiagnosticsFields::WIFI_CONNECTED] = WiFi.isConnected();
    responseDoc[MqttProtocol::DiagnosticsFields::WIFI][MqttProtocol::DiagnosticsFields::WIFI_RSSI] = WiFi.RSSI();
    
    SensorData sensorData = sensorManager.getCurrentData();
    responseDoc[MqttProtocol::DiagnosticsFields::SENSORS][MqttProtocol::DiagnosticsFields::SENSOR_TEMPERATURE] = sensorData.inTemp;
    responseDoc[MqttProtocol::DiagnosticsFields::SENSORS][MqttProtocol::DiagnosticsFields::SENSOR_HUMIDITY] = sensorData.inHumidity;
    responseDoc[MqttProtocol::DiagnosticsFields::SENSORS][MqttProtocol::DiagnosticsFields::SENSOR_RISE] = sensorData.currentRisePercent;
    
    if (mqttTopics && mqttManager.publish(mqttTopics->getDiagnosticsResponseTopic().c_str(), responseDoc)) {
        LOG_I(TAG, "Diagnostics response sent");
    } else {
        LOG_E(TAG, "Failed to send diagnostics response");
    }
}

void handleStateOtaUpdate() {
    if (!otaManager.isInProgress()) {
        LOG_W(TAG, "OTA state active but no update in progress, returning to normal operation");
        stateMachine.transitionTo(STATE_SENSING);
        return;
    }
    
    static uint8_t lastDisplayedProgress = 255;
    uint8_t currentProgress = otaManager.getProgress();
    
    if (lastDisplayedProgress == 255 || 
        (currentProgress - lastDisplayedProgress >= 10) || 
        currentProgress == 100) {
        
        display.fillScreen(DisplayConstants::COLOR_WHITE);
        display.setTextColor(DisplayConstants::COLOR_BLACK);
        display.setTextSize(2);
        
        display.setCursor(10, 30);
        display.print("OTA Update");
        
        display.setCursor(10, 60);
        display.print("Progress: ");
        display.print(currentProgress);
        display.print("%");
        
        display.drawRect(10, 80, 276, 20, DisplayConstants::COLOR_BLACK);
        display.fillRect(10, 80, (276 * currentProgress) / 100, 20, DisplayConstants::COLOR_BLACK);
        
        display.updateDisplay();
        lastDisplayedProgress = currentProgress;
    }
    
    auto status = otaManager.getStatus();
    if (status == OtaManager::OtaStatus::ERROR) {
        LOG_E(TAG, "OTA error occurred, returning to normal operation");
        display.fillScreen(DisplayConstants::COLOR_WHITE);
        display.setTextColor(DisplayConstants::COLOR_RED);
        display.setCursor(10, 150);
        display.print("OTA Failed!");
        display.updateDisplay();
        TimeUtils::delay_for(std::chrono::seconds(3));
        stateMachine.transitionTo(STATE_SENSING);
    } else if (status == OtaManager::OtaStatus::COMPLETE) {
        LOG_I(TAG, "OTA complete, device will reboot");
        display.fillScreen(DisplayConstants::COLOR_WHITE);
        display.setTextColor(DisplayConstants::COLOR_BLACK);
        display.setCursor(10, 150);
        display.print("Update Complete!");
        display.setCursor(10, 180);
        display.print("Rebooting...");
        display.updateDisplay();
    }
}

void setupOtaHandler() {
    if (!mqttTopics) {
        LOG_E(TAG, "Cannot setup OTA - topics not initialized");
        return;
    }
    
    otaManager.begin();
    
    otaManager.setBatteryCheckCallback([]() {
        // TODO: Integrer med battery manager når det er implementeret
        // Skal tjekke om batteriniveau er over sikker grænse (f.eks. 30%)
        // Indtil videre tillader vi altid OTA
        return true;
    });
    
    mqttManager.setCallback(handleOtaMessage);
    
    if (mqttManager.subscribe(mqttTopics->getOtaStartTopic().c_str())) {
        LOG_I(TAG, "Subscribed to OTA start topic: %s", 
              mqttTopics->getOtaStartTopic().c_str());
    }
    
    if (mqttManager.subscribe(mqttTopics->getOtaChunkTopic().c_str())) {
        LOG_I(TAG, "Subscribed to OTA chunk topic: %s", 
              mqttTopics->getOtaChunkTopic().c_str());
    }
}

void handleOtaMessage(char* topic, byte* payload, unsigned int length) {
    if (!mqttTopics) return;
    
    String topicStr(topic);
    
    if (topicStr == mqttTopics->getOtaStartTopic()) {
        LOG_I(TAG, "Received OTA start command");
        
        DynamicJsonDocument doc(256);
        DeserializationError error = deserializeJson(doc, payload, length);
        
        if (error) {
            LOG_E(TAG, "Failed to parse OTA start message: %s", error.c_str());
            return;
        }
        
        OtaManager::OtaInfo info;
        info.version = doc[MqttProtocol::OtaFields::VERSION].as<String>();
        info.size = doc[MqttProtocol::OtaFields::SIZE];
        info.crc32 = doc[MqttProtocol::OtaFields::CRC32];
        
        if (otaManager.startUpdate(info)) {
            stateMachine.transitionTo(STATE_OTA_UPDATE);
            
            DynamicJsonDocument statusDoc(128);
            statusDoc[MqttProtocol::OtaFields::STATUS] = MqttProtocol::OtaFields::StatusValues::STARTED;
            statusDoc[MqttProtocol::OtaFields::VERSION] = info.version;
            mqttManager.publish(mqttTopics->getOtaStatusTopic().c_str(), statusDoc);
        } else {
            DynamicJsonDocument statusDoc(128);
            statusDoc[MqttProtocol::OtaFields::STATUS] = MqttProtocol::OtaFields::StatusValues::ERROR;
            statusDoc[MqttProtocol::OtaFields::MESSAGE] = "Failed to start OTA";
            mqttManager.publish(mqttTopics->getOtaStatusTopic().c_str(), statusDoc);
        }
    }
    else if (topicStr == mqttTopics->getOtaChunkTopic()) {
        if (length < 8) {
            LOG_E(TAG, "Invalid OTA chunk - too small");
            return;
        }
        
        uint32_t chunkIndex, chunkSize;
        memcpy(&chunkIndex, payload, 4);
        memcpy(&chunkSize, payload + 4, 4);
        
        const uint8_t* data = payload + 8;
        size_t dataLength = length - 8;
        
        if (!otaManager.processChunk(data, dataLength, chunkIndex, chunkSize)) {
            LOG_E(TAG, "Failed to process OTA chunk %d", chunkIndex);
            stateMachine.transitionTo(STATE_SENSING);
        }
        
        if (chunkIndex % 10 == 0) {
            DynamicJsonDocument statusDoc(128);
            statusDoc[MqttProtocol::OtaFields::STATUS] = MqttProtocol::OtaFields::StatusValues::DOWNLOADING;
            statusDoc[MqttProtocol::OtaFields::PROGRESS] = otaManager.getProgress();
            mqttManager.publish(mqttTopics->getOtaStatusTopic().c_str(), statusDoc);
        }
    }
}