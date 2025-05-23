#pragma once

#include <WiFiClientSecure.h>
#include <WiFiClient.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>

class MqttManager {
  private:
    WiFiClientSecure _wifiClientSecure;
    WiFiClient _wifiClient;
    PubSubClient _mqttClient;

    String _server;
    int _port;
    String _user;
    String _password;
    String _clientId;

    unsigned long lastReconnectAttempt;

    bool reconnect();

  public:
    MqttManager();
    bool begin(const char* server, int port, const char* user, const char* password, const char* clientId);
    void loop();
    bool isConnected();

    bool publish(const char* topic, const JsonDocument& data);
    bool publish(const char* topic, const char* payload);
};