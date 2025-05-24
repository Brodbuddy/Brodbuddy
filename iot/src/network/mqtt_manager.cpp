#include "network/mqtt_manager.h"

#include "config/time_utils.h"
#include "logging/logger.h"

static const char* TAG = "MqttManager";

MqttManager::MqttManager() : _mqttClient(_wifiClientSecure), lastReconnectAttempt(0) {}

bool MqttManager::begin(const char* server, int port, const char* user, const char* password, const char* clientId) {
    _server = server;
    _port = port;
    _user = user;
    _password = password;
    _clientId = clientId;

    _wifiClientSecure.setInsecure();
    _mqttClient.setServer(server, port);
    _mqttClient.setKeepAlive(TimeUtils::to_seconds(TimeConstants::MQTT_KEEP_ALIVE));
    _mqttClient.setSocketTimeout(TimeUtils::to_seconds(TimeConstants::MQTT_SOCKET_TIMEOUT_DURATION));

    return reconnect();
}

void MqttManager::loop() {
    if (!_mqttClient.connected()) {
        unsigned long currentMillis = millis();
        if (currentMillis - lastReconnectAttempt > TimeUtils::to_ms(TimeConstants::MQTT_RETRY_DELAY)) {
            lastReconnectAttempt = currentMillis;
            if (reconnect()) {
                lastReconnectAttempt = 0;
            }
        }
    } else {
        _mqttClient.loop();
    }
}

bool MqttManager::isConnected() {
    return _mqttClient.connected();
}

bool MqttManager::publish(const char* topic, const JsonDocument& data) {
    String json;
    serializeJson(data, json);
    LOG_D(TAG, "Publishing JSON to %s: %s", topic, json.c_str());
    return publish(topic, json.c_str());
}

bool MqttManager::publish(const char* topic, const char* payload) {
    if (!isConnected()) {
        LOG_W(TAG, "Not connected, cannot publish to %s", topic);
        return false;
    }

    LOG_D(TAG, "Publishing to %s: %s", topic, payload);
    return _mqttClient.publish(topic, payload);
}

bool MqttManager::reconnect() {
    LOG_I(TAG, "Attempting MQTT connection...");

    if (_mqttClient.connect(_clientId.c_str(), _user.c_str(), _password.c_str())) {
        LOG_I(TAG, "Connected to MQTT broker");
        return true;
    } else {
        LOG_E(TAG, "Failed to connect, rc=%d", _mqttClient.state());
        return false;
    }
}