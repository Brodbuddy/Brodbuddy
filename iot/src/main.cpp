#include <Arduino.h>
#include <ArduinoJson.h>
#include <WiFi.h>

#include "components/wifi_manager.h"
#include "components/mqtt_manager.h"
#include "components/sensor_manager.h"
#include "components/button_manager.h"
#include "components/epaper_monitor.h"
#include "display/epaper_display.h"
#include "state_machine.h"
#include "utils/settings.h"
#include "utils/logger.h"
#include "utils/constants.h"
#include "utils/time_utils.h"
#include "data_types.h"

static const char* TAG = "Main";

WifiManager wifiManager;
MqttManager mqttManager;
StateMachine stateMachine;
SensorManager sensorManager;
ButtonManager buttonManager;
Settings settings;
EpaperDisplay display;
EpaperMonitor monitor(display);

unsigned long lastStateCheck = 0;

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

void setup() {
    Serial.begin(115200);
    Logger::begin(LOG_DEBUG, true);

    LOG_I(TAG, "--- Sourdough analyzer startup monitoring ---");

    if (!settings.begin()) {
        LOG_E(TAG, "Failed to initialize settings");
        stateMachine.transitionTo(STATE_ERROR);
        return;
    }

    display.begin();
    SourdoughData data = monitor.generateMockData();
    monitor.updateDisplay(data);

    if (!sensorManager.begin()) {
        LOG_E(TAG, "Failed to initialize sensors");
    }

    sensorManager.setCalibration(settings.getTempOffset(), settings.getHumOffset());
    sensorManager.setReadInterval(TimeUtils::to_ms(std::chrono::seconds(settings.getSensorInterval())));

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
        stateMachine.transitionTo(STATE_CONNECTING_MQTT);
    }
}

void handleStateConnectingMqtt() {
    if (!mqttManager.isConnected()) {
        String server = settings.getMqttServer();
        int port = settings.getMqttPort();
        String user = settings.getMqttUser();
        String password = settings.getMqttPassword();
        String deviceId = settings.getDeviceId();

        LOG_D(TAG, "Connecting to MQTT - Server: %s, Port: %d, User: %s, Device: %s", server.c_str(), port,
              user.c_str(), deviceId.c_str());

        if (mqttManager.begin(server.c_str(), port, user.c_str(), password.c_str(), deviceId.c_str())) {
            LOG_I(TAG, "MQTT connection established");
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
    if (sensorManager.shouldRead()) {
        LOG_I(TAG, "Collecting sensor samples...");
        if (sensorManager.collectMultipleSamples()) {
            LOG_I(TAG, "Sensor reading complete");
            stateMachine.transitionTo(STATE_UPDATING_DISPLAY);
        }
    }
}

void handleStateUpdatingDisplay() {
    display.updateDisplay();
    LOG_I(TAG, "Display updated");
    stateMachine.transitionTo(STATE_PUBLISHING_DATA);
}

void handleStatePublishingData() {
    if (!mqttManager.isConnected()) {
        stateMachine.transitionTo(STATE_CONNECTING_MQTT);
    } else {
        DynamicJsonDocument doc(256);
        SensorData data = sensorManager.getCurrentData();
        doc["temperature"] = data.inTemp;
        doc["humidity"] = data.inHumidity;
        doc["rise"] = data.currentRisePercent;

        if (mqttManager.publish("kakao/maelk", doc)) {
            LOG_I(TAG, "Data published");
        }
        stateMachine.transitionTo(STATE_SLEEP);
    }
}

void handleStateSleep() {
    if (settings.getLowPowerMode()) {
        WiFi.disconnect(true);
        WiFi.mode(WIFI_OFF);

        TimeUtils::enable_sleep_timer(std::chrono::seconds(settings.getSensorInterval()));
        esp_light_sleep_start();

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
            stateMachine.transitionTo(STATE_SENSING);
        }
    }
}

void handleStateError() {
    LOG_E(TAG, "System in error state");
    TimeUtils::delay_for(TimeConstants::ERROR_STATE_DELAY);
    ESP.restart();
}