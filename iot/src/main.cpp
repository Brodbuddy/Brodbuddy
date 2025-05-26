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
#include "network/mqtt_message_router.h"
#include "logging/logger.h"
#include "app/ntfy_manager.h"

static const char* TAG = "Main";

WifiManager wifiManager;
MqttManager mqttManager;
MqttTopics* mqttTopics = nullptr;
MqttMessageRouter messageRouter;
TimeManager timeManager;
StateMachine stateMachine;
SensorManager sensorManager;
ButtonManager buttonManager;
Settings settings;
EpaperDisplay display;
EpaperMonitor monitor(display);
SourdoughData historicalData = {};
OtaManager otaManager;
NtfyManager* ntfyManager = nullptr;

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
void handleDiagnosticsRequest(const String& topic, const uint8_t* payload, unsigned int length);
void handleOtaMessageWrapper(const String& topic, const uint8_t* payload, unsigned int length);
void setupDiagnosticsHandler();
void setupOtaHandler();
void validateBootAfterOta();
void publishOtaStatus(const String& status, uint8_t progress);

void setup() {
    Serial.begin(115200);
    Logger::begin(LOG_INFO, true, true);

    LOG_I(TAG, "--- Sourdough analyzer startup ---");
    
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

    String analyzerId = settings.getAnalyzerId();
    ntfyManager = new NtfyManager(analyzerId);
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

void validateBootAfterOta() {
    const esp_partition_t* runningPartition = esp_ota_get_running_partition();
    esp_ota_img_states_t otaState;
    
    if (esp_ota_get_state_partition(runningPartition, &otaState) == ESP_OK) {
        if (otaState == ESP_OTA_IMG_PENDING_VERIFY) {
            LOG_I(TAG, "First boot after OTA update, validation pending...");
            LOG_I(TAG, "Running from partition: %s", runningPartition->label);
            needsOtaValidation = true;
        }
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
            timeManager.trySync();
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

        LOG_D(TAG, "Connecting to MQTT - Server: %s, Port: %d", server.c_str(), port);
              
        if (mqttTopics == nullptr) {
            mqttTopics = new MqttTopics(analyzerId);
            mqttManager.setTopics(mqttTopics);
            messageRouter.setTopics(mqttTopics);
            LOG_I(TAG, "MQTT topics initialized for device: %s", analyzerId.c_str());
        }

        if (mqttManager.begin(server.c_str(), port, user.c_str(), password.c_str(), analyzerId.c_str())) {
            LOG_I(TAG, "MQTT connection established");
            
            if (needsOtaValidation) {
                LOG_I(TAG, "Marking OTA update as valid");
                esp_ota_mark_app_valid_cancel_rollback();
                needsOtaValidation = false;
            }
            
            mqttManager.setCallback([](char* topic, byte* payload, unsigned int length) {
                messageRouter.routeMessage(topic, payload, length);
            });
            
            messageRouter.setDiagnosticsHandler(handleDiagnosticsRequest);
            messageRouter.setOtaHandler(handleOtaMessageWrapper);
            
            setupDiagnosticsHandler();
            setupOtaHandler();
            
            timeManager.trySync();
            
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
    if (timeManager.shouldRetrySync()) {
        timeManager.trySync();
    }
    
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
            
            if (ntfyManager) {
                ntfyManager->checkRiseValue(sensorData.currentRisePercent);
            }
            
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
        
        mqttManager.loop();
        
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
        
        stateMachine.transitionTo(STATE_CONNECTING_WIFI);
    } else {
        stateMachine.transitionTo(STATE_SENSING);
    }
}

void handleStateError() {
    LOG_E(TAG, "Error state - resetting in 5 seconds");
    TimeUtils::delay_for(TimeConstants::ERROR_STATE_DELAY);
    ESP.restart();
}

void handleStateOtaUpdate() {
    using namespace std::chrono;
    
    static steady_clock::time_point otaStartTime;
    static steady_clock::time_point lastMqttLoop;
    static steady_clock::time_point lastProgressTime;
    static uint8_t lastProgress = 0;
    static bool wasConnected = true;
    static bool initialized = false;
    
    auto now = steady_clock::now();
    
    if (!initialized) {
        otaStartTime = now;
        lastMqttLoop = now;
        lastProgressTime = now;
        initialized = true;
        LOG_I(TAG, "OTA Update state entered");
    }
    
    bool isConnected = mqttManager.isConnected();
    if (!wasConnected && isConnected) {
        LOG_W(TAG, "MQTT reconnected during OTA - re-subscribing");
        setupOtaHandler();
        
        if (mqttTopics) {
            DynamicJsonDocument statusDoc(128);
            statusDoc[MqttProtocol::OtaFields::STATUS] = MqttProtocol::OtaFields::StatusValues::DOWNLOADING;
            statusDoc[MqttProtocol::OtaFields::PROGRESS] = otaManager.getProgress();
            statusDoc["resumed"] = true;
            mqttManager.publish(mqttTopics->getOtaStatusTopic().c_str(), statusDoc);
        }
    }
    wasConnected = isConnected;
    
    if (isConnected && duration_cast<milliseconds>(now - lastMqttLoop) > TimeConstants::OTA_MQTT_LOOP_INTERVAL) {
        mqttManager.loop();
        lastMqttLoop = now;
    }
    
    if (!otaManager.isInProgress()) {
        LOG_W(TAG, "OTA state active but no update in progress");
        stateMachine.transitionTo(STATE_SENSING);
        initialized = false;
        return;
    }
    
    uint8_t currentProgress = otaManager.getProgress();
    if (currentProgress != lastProgress) {
        lastProgress = currentProgress;
        lastProgressTime = now;
        LOG_I(TAG, "OTA Progress: %d%%", currentProgress);
    }
    
    auto timeSinceProgress = duration_cast<seconds>(now - lastProgressTime);
    
    if (timeSinceProgress > TimeConstants::OTA_PROGRESS_STALL_WARNING && currentProgress > 0) {
        if (timeSinceProgress > TimeConstants::OTA_PROGRESS_STALL_TIMEOUT) {
            LOG_E(TAG, "OTA timeout - stuck at %d%%", currentProgress);
            otaManager.abort("OTA stalled");
            stateMachine.transitionTo(STATE_SENSING);
            initialized = false;
            return;
        }
    }
    
    auto timeInOta = duration_cast<seconds>(now - otaStartTime);
    if (timeInOta > TimeConstants::OTA_INITIAL_TIMEOUT && currentProgress == 0) {
        LOG_E(TAG, "OTA timeout - no chunks received");
        otaManager.abort("No chunks received");
        stateMachine.transitionTo(STATE_SENSING);
        initialized = false;
        return;
    }
    
    auto status = otaManager.getStatus();
    if (status == OtaManager::OtaStatus::ERROR) {
        LOG_E(TAG, "OTA error occurred");
        stateMachine.transitionTo(STATE_SENSING);
        initialized = false;
    } else if (status == OtaManager::OtaStatus::COMPLETE) {
        LOG_I(TAG, "OTA complete, device will reboot");
        initialized = false;
    }
}

void setupDiagnosticsHandler() {
    if (!mqttTopics) {
        LOG_E(TAG, "Cannot setup diagnostics - topics not initialized");
        return;
    }
    
    if (mqttManager.subscribe(mqttTopics->getDiagnosticsRequestTopic().c_str())) {
        LOG_I(TAG, "Diagnostics handler registered");
    }
}

void handleDiagnosticsRequest(const String& topic, const uint8_t* payload, unsigned int length) {
    LOG_I(TAG, "Diagnostics request received");
    
    DynamicJsonDocument responseDoc(1024);
    
    responseDoc[MqttProtocol::DiagnosticsFields::ANALYZER_ID] = settings.getAnalyzerId();
    responseDoc[MqttProtocol::DiagnosticsFields::UPTIME] = millis();
    responseDoc[MqttProtocol::DiagnosticsFields::FREE_HEAP] = ESP.getFreeHeap();
    responseDoc[MqttProtocol::DiagnosticsFields::WIFI_RSSI] = WiFi.RSSI();
    responseDoc[MqttProtocol::DiagnosticsFields::STATE] = stateMachine.getStateName();
    
    SensorData sensorData = sensorManager.getCurrentData();
    responseDoc[MqttProtocol::DiagnosticsFields::SENSORS][MqttProtocol::DiagnosticsFields::SENSOR_TEMPERATURE] = sensorData.inTemp;
    responseDoc[MqttProtocol::DiagnosticsFields::SENSORS][MqttProtocol::DiagnosticsFields::SENSOR_HUMIDITY] = sensorData.inHumidity;
    responseDoc[MqttProtocol::DiagnosticsFields::SENSORS][MqttProtocol::DiagnosticsFields::SENSOR_RISE] = sensorData.currentRisePercent;
    
    if (mqttTopics && mqttManager.publish(mqttTopics->getDiagnosticsResponseTopic().c_str(), responseDoc)) {
        LOG_I(TAG, "Diagnostics response sent");
    }
}

void setupOtaHandler() {
    if (!mqttTopics) {
        LOG_E(TAG, "Cannot setup OTA - topics not initialized");
        return;
    }
    
    otaManager.begin();
    
    otaManager.setBatteryCheckCallback([]() {
        return true;
    });
    
    otaManager.setStatusCallback(publishOtaStatus);
    
    String otaStartTopic = mqttTopics->getOtaStartTopic();
    String otaChunkTopic = mqttTopics->getOtaChunkTopic();
    
    if (mqttManager.subscribe(otaStartTopic.c_str())) {
        LOG_I(TAG, "Subscribed to OTA start topic");
    }
    
    if (mqttManager.subscribe(otaChunkTopic.c_str())) {
        LOG_I(TAG, "Subscribed to OTA chunk topic");
    }
}

void handleOtaMessageWrapper(const String& topic, const uint8_t* payload, unsigned int length) {
    if (!mqttTopics) return;
    
    bool handled = otaManager.handleOtaMessage(topic, payload, length, 
                                              mqttTopics->getOtaStartTopic(), 
                                              mqttTopics->getOtaChunkTopic());
    
    if (handled && topic == mqttTopics->getOtaStartTopic()) {
        stateMachine.transitionTo(STATE_OTA_UPDATE);
    }
}

void publishOtaStatus(const String& status, uint8_t progress) {
    if (!mqttTopics || !mqttManager.isConnected()) return;
    
    static unsigned long lastPublish = 0;
    unsigned long now = millis();
    
    if (progress % OtaConstants::PROGRESS_UPDATE_CHUNK_INTERVAL == 0 || 
        now - lastPublish > 5000) {
        
        DynamicJsonDocument statusDoc(128);
        statusDoc[MqttProtocol::OtaFields::STATUS] = status;
        statusDoc[MqttProtocol::OtaFields::PROGRESS] = progress;
        
        mqttManager.publish(mqttTopics->getOtaStatusTopic().c_str(), statusDoc);
        lastPublish = now;
    }
}