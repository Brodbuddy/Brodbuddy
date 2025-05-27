#include "network/mqtt_manager.h"

#include "config/constants.h"
#include "config/time_utils.h"
#include "logging/logger.h"

static const char* TAG = "MqttManager";

MqttManager* MqttManager::_instance = nullptr;
std::function<void(char*, byte*, unsigned int)> _userCallback = nullptr;

MqttManager::MqttManager() : _mqttClient(_wifiClient), _topics(nullptr), lastReconnectAttempt(0) {
    _instance = this;
}

bool MqttManager::begin(const char* server, int port, const char* user, const char* password, const char* clientId) {
    _server = server;
    _port = port;
    _user = user;
    _password = password;
    _clientId = clientId;

    _wifiClientSecure.setInsecure();
    _mqttClient.setServer(server, port);
    _mqttClient.setBufferSize(NetworkConstants::MQTT_BUFFER_SIZE);
    _mqttClient.setKeepAlive(TimeUtils::to_seconds(TimeConstants::MQTT_KEEP_ALIVE));
    _mqttClient.setSocketTimeout(TimeUtils::to_seconds(TimeConstants::MQTT_SOCKET_TIMEOUT_DURATION));
    _mqttClient.setCallback(mqttCallback);

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

bool MqttManager::subscribe(const char* topic) {
    if (!isConnected()) {
        LOG_W(TAG, "Not connected, cannot subscribe to %s", topic);
        return false;
    }
    
    bool result = _mqttClient.subscribe(topic);
    if (result) {
        LOG_I(TAG, "Subscribed to topic: %s", topic);
    } else {
        LOG_E(TAG, "Failed to subscribe to topic: %s", topic);
    }
    return result;
}

void MqttManager::setCallback(std::function<void(char*, byte*, unsigned int)> callback) {
    _userCallback = callback;
}

void MqttManager::mqttCallback(char* topic, byte* payload, unsigned int length) {
    if (_userCallback) {
        _userCallback(topic, payload, length);
    }
}

bool MqttManager::reconnect() {
    LOG_I(TAG, "Attempting MQTT connection to %s:%d", _server.c_str(), _port);
    
    // Ensure server is still configured (in case of memory corruption)
    if (_server.length() == 0) {
        LOG_E(TAG, "Server hostname is empty, cannot reconnect!");
        return false;
    }
    
    // Re-set server in case connection was corrupted
    _mqttClient.setServer(_server.c_str(), _port);

    if (_mqttClient.connect(_clientId.c_str(), _user.c_str(), _password.c_str())) {
        LOG_I(TAG, "Connected to MQTT broker");
        
        // IMPORTANT: Need to re-subscribe after reconnection
        // This is handled by the main code that calls reconnect
        // But we should notify that subscriptions are lost
        LOG_W(TAG, "MQTT reconnected - subscriptions need to be restored!");
        
        return true;
    } else {
        LOG_E(TAG, "Failed to connect, rc=%d", _mqttClient.state());
        return false;
    }
}