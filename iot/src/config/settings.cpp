#include "config/settings.h"

#include "logging/logger.h"

const char* Settings::SETTINGS_FILE = "/settings.json";

Settings::Settings() : _doc(1024), _loaded(false) {}

bool Settings::begin() {
    if (!LittleFS.begin(true)) {
        LOG_E("Settings", "Failed to mount LittleFS");
        return false;
    }

    if (!load()) {
        LOG_W("Settings", "No settings file found, creating defaults");
        setDefaults();
        return save();
    }

    return true;
}

bool Settings::save() {
    return saveToFile();
}

bool Settings::load() {
    return loadFromFile();
}

void Settings::setDefaults() {
    _doc.clear();
    _doc["analyzerId"] = "brodbuddy_01";
    _doc["mqtt"]["server"] = "broker.hivemq.com";
    _doc["mqtt"]["port"] = 1883;
    _doc["mqtt"]["user"] = "";
    _doc["mqtt"]["password"] = "";
    _doc["sensor"]["intervalSeconds"] = 900;
    _doc["display"]["intervalSeconds"] = 300;
    _doc["lowPowerMode"] = true;
    _doc["calibration"]["tempOffsetCelsius"] = -1.7;
    _doc["calibration"]["humOffset"] = 7.3;
    _doc["calibration"]["containerHeightMillis"] = 200;
    _loaded = true;
}

bool Settings::saveToFile() {
    File file = LittleFS.open(SETTINGS_FILE, "w");
    if (!file) {
        LOG_E("Settings", "Failed to open settings for writing");
        return false;
    }

    serializeJson(_doc, file);
    file.close();

    LOG_I("Settings", "Settings saved successfully");
    return true;
}

bool Settings::loadFromFile() {
    if (!LittleFS.exists(SETTINGS_FILE)) {
        return false;
    }

    File file = LittleFS.open(SETTINGS_FILE, "r");
    if (!file) {
        LOG_E("Settings", "Failed to open settings file for reading");
        return false;
    }

    DeserializationError error = deserializeJson(_doc, file);
    file.close();

    if (error) {
        LOG_E("Settings", "Failed to parse settings: %s", error.c_str());
        return false;
    }

    _loaded = true;
    LOG_I("Settings", "Settings loaded successfully");
    return _loaded;
}

String Settings::getAnalyzerId() const {
    return _doc["analyzerId"].as<String>();
}

void Settings::setAnalyzerId(const String& id) {
    _doc["analyzerId"] = id;
}

String Settings::getMqttServer() const {
    return _doc["mqtt"]["server"].as<String>();
}

void Settings::setMqttServer(const String& server) {
    _doc["mqtt"]["server"] = server;
}

int Settings::getMqttPort() const {
    return _doc["mqtt"]["port"].as<int>();
}

void Settings::setMqttPort(int port) {
    _doc["mqtt"]["port"] = port;
}

String Settings::getMqttUser() const {
    return _doc["mqtt"]["user"].as<String>();
}

void Settings::setMqttUser(const String& user) {
    _doc["mqtt"]["user"] = user;
}

String Settings::getMqttPassword() const {
    String password = _doc["mqtt"]["password"].as<String>();

    if (password == "${MQTT_PASSWORD}") {
#ifdef MQTT_PASSWORD
        return String(MQTT_PASSWORD);
#else
        LOG_W("Settings", "MQTT_PASSWORD not defined at compile time!");
        return "";
#endif
    }

    return password;
}

void Settings::setMqttPassword(const String& password) {
    _doc["mqtt"]["password"] = password;
}

int Settings::getSensorInterval() const {
    return _doc["sensor"]["intervalSeconds"].as<int>();
}

void Settings::setSensorInterval(int seconds) {
    _doc["sensor"]["intervalSeconds"] = seconds;
}

int Settings::getDisplayInterval() const {
    return _doc["display"]["intervalSeconds"].as<int>();
}

void Settings::setDisplayInterval(int seconds) {
    _doc["display"]["intervalSeconds"] = seconds;
}

bool Settings::getLowPowerMode() const {
    return _doc["lowPowerMode"].as<bool>();
}

void Settings::setLowPowerMode(bool enabled) {
    _doc["lowPowerMode"] = enabled;
}

float Settings::getTempOffset() const {
    return _doc["calibration"]["tempOffsetCelsius"].as<float>();
}

void Settings::setTempOffset(float offset) {
    _doc["calibration"]["tempOffsetCelsius"] = offset;
}

float Settings::getHumOffset() const {
    return _doc["calibration"]["humOffset"].as<float>();
}

void Settings::setHumOffset(float offset) {
    _doc["calibration"]["humOffset"] = offset;
}

int Settings::getContainerHeight() const {
    return _doc["calibration"]["containerHeightMillis"].as<int>();
}

void Settings::setContainerHeight(int height) {
    _doc["calibration"]["containerHeightMillis"] = height;
}

void Settings::printSettings() const {
    serializeJsonPretty(_doc, Serial);
    Serial.println();
}