#pragma once

#include <WebServer.h>
#include <DNSServer.h>
#include <ArduinoJson.h>

#include "app/data_types.h"
#include "config/constants.h"
#include "logging/logger.h"
#include "network/captive_portal_html.h"

class CaptivePortalManager {
  public:
    CaptivePortalManager();
    ~CaptivePortalManager();

    void startCustomPortal();
    void stopCustomPortal();
    void loop();

    bool isRunning() const { return portalRunning; }
    String getLastConnectionStatus() const { return lastConnectionStatus; }

    void setSaveCredentialsCallback(std::function<void(const String&, const String&)> callback) {
        saveCredentialsCallback = callback;
    }

    void setStatusCallback(std::function<void(int)> callback) { statusCallback = callback; }

    void setBlinkTaskCallback(std::function<void()> callback) { blinkTaskCallback = callback; }

  private:
    WebServer* server;
    DNSServer* dns;
    bool portalRunning;
    String lastConnectionStatus;

    unsigned long apModeStartTime;
    bool apModeTimeoutEnabled;

    std::function<void(const String&, const String&)> saveCredentialsCallback;
    std::function<void(int)> statusCallback;
    std::function<void()> blinkTaskCallback;

    void setupWebServer();
    void handleRoot();
    void handleScan();
    void handleConnect();
    void handleNotFound();
    void handleConnectionStatus();
};