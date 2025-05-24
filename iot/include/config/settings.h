#ifndef SETTINGS_H
#define SETTINGS_H

#include <Arduino.h>
#include <ArduinoJson.h>
#include <LittleFS.h>

class Settings {
  private:
    static const char* SETTINGS_FILE;
    DynamicJsonDocument _doc;
    bool _loaded;

    bool saveToFile();
    bool loadFromFile();

  public:
    Settings();

    bool begin();
    bool save();
    bool load();
    void setDefaults();

    String getDeviceId() const;
    void setDeviceId(const String& id);

    String getMqttServer() const;
    void setMqttServer(const String& server);

    int getMqttPort() const;
    void setMqttPort(int port);

    String getMqttUser() const;
    void setMqttUser(const String& user);

    String getMqttPassword() const;
    void setMqttPassword(const String& password);

    int getSensorInterval() const;
    void setSensorInterval(int seconds);

    int getDisplayInterval() const;
    void setDisplayInterval(int seconds);

    bool getLowPowerMode() const;
    void setLowPowerMode(bool enabled);

    float getTempOffset() const;
    void setTempOffset(float offset);

    float getHumOffset() const;
    void setHumOffset(float offset);

    int getContainerHeight() const;
    void setContainerHeight(int height);

    void printSettings() const;
};

#endif