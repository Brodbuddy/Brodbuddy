#ifndef MQTT_PROTOCOL_H
#define MQTT_PROTOCOL_H

namespace MqttProtocol {
    namespace TelemetryFields {
        constexpr const char* ANALYZER_ID = "analyzerId";
        constexpr const char* EPOCH_TIME = "epochTime";
        constexpr const char* TIMESTAMP = "timestamp";
        constexpr const char* LOCAL_TIME = "localTime";
        constexpr const char* TEMPERATURE = "temperature";
        constexpr const char* HUMIDITY = "humidity";
        constexpr const char* RISE = "rise";
    }
    
    namespace DiagnosticsFields {
        constexpr const char* ANALYZER_ID = "analyzerId";
        constexpr const char* EPOCH_TIME = "epochTime";
        constexpr const char* TIMESTAMP = "timestamp";
        constexpr const char* LOCAL_TIME = "localTime";
        constexpr const char* UPTIME = "uptime";
        constexpr const char* FREE_HEAP = "freeHeap";
        constexpr const char* STATE = "state";
        constexpr const char* WIFI = "wifi";
        constexpr const char* WIFI_CONNECTED = "connected";
        constexpr const char* WIFI_RSSI = "rssi";
        constexpr const char* SENSORS = "sensors";
        constexpr const char* SENSOR_TEMPERATURE = "temperature";
        constexpr const char* SENSOR_HUMIDITY = "humidity";
        constexpr const char* SENSOR_RISE = "rise";
    }
}

#endif