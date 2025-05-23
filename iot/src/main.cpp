#include <Arduino.h>
#include <ArduinoJson.h>
#include <WiFi.h>

#include "components/wifi_manager.h"
#include "components/mqtt_manager.h"
#include "components/sensor_manager.h"
#include "components/SourdoughMonitor.h"
#include "display/SourdoughDisplay.h"
#include "state_machine.h"
#include "utils/settings.h"
#include "utils/logger.h"
#include "data_types.h"

static const char* TAG = "Main";

BroadBuddyWiFiManager wifiManager;
MqttManager mqttManager;
StateMachine stateMachine;
SensorManager sensorManager;
Settings settings;
SourdoughDisplay display;
SourdoughMonitor monitor(display);

unsigned long lastStateCheck = 0;
const unsigned long STATE_CHECK_INTERVAL = 100;

void setup()
{
  Serial.begin(115200);
  Logger::begin(LOG_INFO);

  LOG_I(TAG, "--- Sourdough analyzer startup monitoring ---");

  if (!settings.begin()) {
    LOG_E(TAG, "Failed to initialize settings");
    stateMachine.transitionTo(STATE_ERROR);
    return;
  }

  //display.begin();
  SourdoughData data = monitor.generateMockData();
  monitor.updateDisplay(data);

  if (!sensorManager.begin()) {
    LOG_E(TAG, "Failed to initialize sensors");
  }

  sensorManager.setCalibration(
    settings.getTempOffset(),
    settings.getHumOffset()
  );
  sensorManager.setReadInterval(settings.getSensorInterval() * 1000);

  wifiManager.setup();

  stateMachine.transitionTo(STATE_CONNECTING_WIFI);
}

void loop()
{
  unsigned long currentMillis = millis();
  LOG_D(TAG, "currentMillis: %lu", currentMillis);

  if (currentMillis - lastStateCheck >= STATE_CHECK_INTERVAL) { 
    lastStateCheck = currentMillis;

    switch (stateMachine.getCurrentState()) {
      case STATE_BOOT:
        stateMachine.transitionTo(STATE_CONNECTING_WIFI);
        break;

      case STATE_CONNECTING_WIFI:
          wifiManager.loop();
          if (wifiManager.getStatus() == WIFI_CONNECTED) {
            delay(1000);
            LOG_I(TAG, "WiFi connected");
            stateMachine.transitionTo(STATE_CONNECTING_MQTT);
          }
          break;

      case STATE_CONNECTING_MQTT:
        if (!mqttManager.isConnected()) {
          String server = settings.getMqttServer();
          int port = settings.getMqttPort();
          String user = settings.getMqttUser();
          String password = settings.getMqttPassword();
          String deviceId = settings.getDeviceId();
          
          LOG_I(TAG, "Connecting to MQTT - Server: %s, Port: %d, User: %s, Device: %s", 
                server.c_str(), port, user.c_str(), deviceId.c_str());
          
          if (mqttManager.begin(server.c_str(), port, user.c_str(), password.c_str(), deviceId.c_str())) {
            LOG_I(TAG, "MQTT connection established");
            stateMachine.transitionTo(STATE_SENSING);
          } else {
            LOG_E(TAG, "MQTT connection failed, retrying in 5 seconds");
            delay(5000);
          }
        } else {
          stateMachine.transitionTo(STATE_SENSING);
        }
        break;

      case STATE_SENSING:
        if (sensorManager.shouldRead()) {
          if (sensorManager.readAllSensors()) {
            LOG_I(TAG, "Sensor reading complete");
            stateMachine.transitionTo(STATE_UPDATING_DISPLAY);
          }
        }
        break;

      case STATE_UPDATING_DISPLAY:
        display.updateDisplay();
        LOG_I(TAG, "Display updated");
        stateMachine.transitionTo(STATE_PUBLISHING_DATA);
        break;

      case STATE_PUBLISHING_DATA:
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
        break;

      case STATE_SLEEP:
        if (settings.getLowPowerMode()) {
          WiFi.disconnect(true);
          WiFi.mode(WIFI_OFF);
          
          esp_sleep_enable_timer_wakeup(settings.getSensorInterval() * 1000000);
          esp_light_sleep_start();
          
          if (!settings.begin()) {
            LOG_E(TAG, "Failed to re-initialize settings after sleep");
            stateMachine.transitionTo(STATE_ERROR);
            break;
          }
          
          WiFi.mode(WIFI_STA);
          WiFi.reconnect();
          delay(1000);
          
          stateMachine.transitionTo(STATE_CONNECTING_WIFI);
        } else {
          if (stateMachine.shouldTransition(settings.getSensorInterval() * 1000)) {
            stateMachine.transitionTo(STATE_SENSING);
          }
        }
        break;

      case STATE_ERROR:
        LOG_E(TAG, "System in error state");
        delay(5000);
        ESP.restart();
        break;
    }
  }

  mqttManager.loop();
}