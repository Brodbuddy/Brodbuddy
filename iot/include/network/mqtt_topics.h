#ifndef MQTT_TOPICS_H
#define MQTT_TOPICS_H

#include <Arduino.h>

class MqttTopics {
private:
    String _analyzerId;
    static constexpr const char* BASE_TOPIC = "analyzer";
    
public:
    explicit MqttTopics(const String& analyzerId) : _analyzerId(analyzerId) {}
    
    String getTelemetryTopic() const {
        return String(BASE_TOPIC) + "/" + _analyzerId + "/telemetry";
    }

    String getDiagnosticsRequestTopic() const {
        return String(BASE_TOPIC) + "/" + _analyzerId + "/diagnostics/request";
    }
    
    String getDiagnosticsResponseTopic() const {
        return String(BASE_TOPIC) + "/" + _analyzerId + "/diagnostics/response";
    }
    
    void updateAnalyzerId(const String& analyzerId) {
        _analyzerId = analyzerId;
    }
};

#endif