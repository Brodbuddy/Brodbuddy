#include "network/mqtt_message_router.h"
#include "logging/logger.h"
#include "config/constants.h"
#include "app/state_machine.h"

const char* MqttMessageRouter::TAG = "MqttRouter";

MqttMessageRouter::MqttMessageRouter() 
    : mqttTopics(nullptr)
    , diagnosticsHandler(nullptr)
    , otaHandler(nullptr)
    , messageCount(0)
    , lastMessageTime(0) {
}

void MqttMessageRouter::routeMessage(char* topic, byte* payload, unsigned int length) {
    messageCount++;
    unsigned long now = millis();
    
    if (messageCount <= 10 || messageCount % OtaConstants::MQTT_MESSAGE_LOG_INTERVAL == 0) {
        LOG_I(TAG, "Message #%lu, topic=%s, length=%d", messageCount, topic, length);
    }
    
    lastMessageTime = now;
    
    if (!mqttTopics) {
        LOG_E(TAG, "Topics not initialized");
        return;
    }
    
    String topicStr(topic);
    
    if (topicStr == mqttTopics->getDiagnosticsRequestTopic()) {
        if (diagnosticsHandler) {
            diagnosticsHandler(topicStr, payload, length);
        }
    } else if (topicStr == mqttTopics->getOtaStartTopic() || 
               topicStr == mqttTopics->getOtaChunkTopic()) {
        if (otaHandler) {
            otaHandler(topicStr, payload, length);
        }
    }
}