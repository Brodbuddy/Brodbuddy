#pragma once

#include <WiFiClientSecure.h>
#include <WiFiClient.h>
#include <PubSubClient.h>
#include <ArduinoJson.h>
#include <functional>

class MqttTopics;

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
    
    MqttTopics* _topics;

    unsigned long lastReconnectAttempt;
    
    static MqttManager* _instance;
    static void mqttCallback(char* topic, byte* payload, unsigned int length);

    bool reconnect();

  public:
    MqttManager();
    bool begin(const char* server, int port, const char* user, const char* password, const char* clientId);
    void loop();
    bool isConnected();

    bool publish(const char* topic, const JsonDocument& data);
    bool publish(const char* topic, const char* payload);
    
    bool subscribe(const char* topic);
    void setCallback(std::function<void(char*, byte*, unsigned int)> callback);
    
    void setTopics(MqttTopics* topics) { _topics = topics; }
    MqttTopics* getTopics() const { return _topics; }
};