#pragma once

#include <Arduino.h>
#include <functional>
#include "network/mqtt_topics.h"

class MqttMessageRouter {
public:
    using MessageHandler = std::function<void(const String& topic, const uint8_t* payload, unsigned int length)>;
    
    MqttMessageRouter();
    
    void setTopics(MqttTopics* topics) { mqttTopics = topics; }
    void setDiagnosticsHandler(MessageHandler handler) { diagnosticsHandler = handler; }
    void setOtaHandler(MessageHandler handler) { otaHandler = handler; }
    
    void routeMessage(char* topic, byte* payload, unsigned int length);
    
private:
    static const char* TAG;
    
    MqttTopics* mqttTopics;
    MessageHandler diagnosticsHandler;
    MessageHandler otaHandler;
    
    unsigned long messageCount;
    unsigned long lastMessageTime;
};