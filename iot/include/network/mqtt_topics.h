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
    
    String getOtaStartTopic() const {
        return String(BASE_TOPIC) + "/" + _analyzerId + "/ota/start";
    }
    
    String getOtaChunkTopic() const {
        return String(BASE_TOPIC) + "/" + _analyzerId + "/ota/chunk";
    }
    
    String getOtaStatusTopic() const {
        return String(BASE_TOPIC) + "/" + _analyzerId + "/ota/status";
    }
    
    String getOtaCheckTopic() const {
        return String(BASE_TOPIC) + "/" + _analyzerId + "/ota/check";
    }
    
    void updateAnalyzerId(const String& analyzerId) {
        _analyzerId = analyzerId;
    }
};

#endif