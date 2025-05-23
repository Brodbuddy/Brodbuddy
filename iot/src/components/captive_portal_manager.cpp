#include "components/captive_portal_manager.h"
#include <WiFi.h>
#include <utils/constants.h>

static const char* TAG = "CaptivePortal";

#define LED_PIN Pins::LED

CaptivePortalManager::CaptivePortalManager() :
  server(nullptr),
  dns(nullptr),
  portalRunning(false),
  apModeStartTime(0),
  apModeTimeoutEnabled(false) {
}

CaptivePortalManager::~CaptivePortalManager() {
  stopCustomPortal();
}

void CaptivePortalManager::startCustomPortal() {
  WiFi.disconnect();
  
  WiFi.mode(WIFI_AP);
  WiFi.softAP(AP_NAME, AP_PASSWORD);
  
  LOG_I(TAG, "AP IP address: %s", WiFi.softAPIP().toString().c_str());
  
  if (dns == nullptr) dns = new DNSServer();
  if (server == nullptr) server = new WebServer(80);
  
  dns->setErrorReplyCode(DNSReplyCode::NoError);
  dns->start(53, "*", WiFi.softAPIP());
  
  setupWebServer();
  
  server->begin();
  portalRunning = true;
  
  if (statusCallback) {
    statusCallback(WIFI_CONNECTING);
  }
  apModeStartTime = millis();
  
  if (blinkTaskCallback) {
    blinkTaskCallback();
  }
  
  LOG_I(TAG, "Portal started on SSID: %s", AP_NAME);
}

void CaptivePortalManager::stopCustomPortal() {
  if (server != nullptr) {
    server->stop();
    delete server;
    server = nullptr;
  }
  
  if (dns != nullptr) {
    dns->stop();
    delete dns;
    dns = nullptr;
  }
  
  
  WiFi.softAPdisconnect(true);
  WiFi.mode(WIFI_STA); 
  
  portalRunning = false;
}

void CaptivePortalManager::loop() {
  if (portalRunning && server != nullptr && dns != nullptr) {
    dns->processNextRequest();
    server->handleClient();
  }
}

void CaptivePortalManager::setupWebServer() {
  server->on("/", HTTP_GET, [this]() {
    this->handleRoot();
  });
  
  server->on("/scan", HTTP_GET, [this]() {
    this->handleScan();
  });
  
  server->on("/connect", HTTP_POST, [this]() {
    this->handleConnect();
  });
  
  server->on("/connection-status", HTTP_GET, [this]() {
    this->handleConnectionStatus();
  });
  
  server->onNotFound([this]() {
    this->handleNotFound();
  });
}

void CaptivePortalManager::handleRoot() {
  server->send(200, "text/html", CAPTIVE_PORTAL_HTML);
}

void CaptivePortalManager::handleScan() {
  int n = WiFi.scanNetworks();
  
  DynamicJsonDocument doc(4096);
  JsonArray networksArray = doc.createNestedArray("networks");
  
  for (int i = 0; i < n; i++) {
    JsonObject network = networksArray.createNestedObject();
    network["ssid"] = WiFi.SSID(i);
    network["rssi"] = WiFi.RSSI(i);
    network["encryption"] = WiFi.encryptionType(i) != WIFI_AUTH_OPEN;
  }
  
  String jsonResponse;
  serializeJson(doc, jsonResponse);
  
  server->send(200, "application/json", jsonResponse);
  
  WiFi.scanDelete();
}

void CaptivePortalManager::handleConnect() {
  String data = server->arg("plain");
  DynamicJsonDocument doc(512);
  DeserializationError error = deserializeJson(doc, data);
  
  if (error) {
    LOG_E(TAG, "JSON parse error: %s", error.c_str());
    server->send(400, "application/json", "{\"success\":false,\"message\":\"Invalid request format\"}");
    return;
  }
  
  String ssid = doc["ssid"];
  String password = doc["password"];
  bool isHidden = doc.containsKey("hidden") ? doc["hidden"].as<bool>() : false;
  
  if (ssid.length() == 0) {
    server->send(400, "application/json", "{\"success\":false,\"message\":\"SSID is required\"}");
    return;
  }
  
  LOG_I(TAG, "Connecting to: %s", ssid.c_str());
  
  if (saveCredentialsCallback) {
    saveCredentialsCallback(ssid, password);
  }
  
  server->send(200, "application/json", "{\"success\":true,\"message\":\"Attempting to connect...\",\"ip\":\"192.168.4.1\"}");
  
  if (isHidden) {
    WiFi.begin(ssid.c_str(), password.c_str(), 0, NULL, true);
  } else {
    WiFi.begin(ssid.c_str(), password.c_str());
  }
  
  int timeout = 30;
  while (WiFi.status() != WL_CONNECTED && timeout > 0) {
    delay(1000);
    LOG_D(TAG, "Connecting...");
    timeout--;
  }
  
  if (WiFi.status() == WL_CONNECTED) {
    LOG_I(TAG, "Connected to WiFi!");
    LOG_I(TAG, "IP address: %s", WiFi.localIP().toString().c_str());
    
    WiFi.setHostname(HOSTNAME);
    
    WiFi.setAutoReconnect(true);
    
    DynamicJsonDocument responseDoc(256);
    responseDoc["success"] = true;
    responseDoc["ip"] = WiFi.localIP().toString();
    responseDoc["message"] = "Forbundet til " + ssid + "! Du kan nu forlade opsætningssiden.";
    responseDoc["disconnectAP"] = true;
    
    String jsonResponse;
    serializeJson(responseDoc, jsonResponse);
    
    lastConnectionStatus = jsonResponse;
    
    if (statusCallback) {
      statusCallback(WIFI_CONNECTED);
    }
    digitalWrite(LED_PIN, HIGH);
    
    delay(1000);
    
    stopCustomPortal();
  } else {
    LOG_E(TAG, "Failed to connect to WiFi");
    
    switch (WiFi.status()) {
      case WL_NO_SSID_AVAIL:
        LOG_E(TAG, "Network not found");
        break;
      case WL_CONNECT_FAILED:
        LOG_E(TAG, "Connection failed - check password");
        break;
      case WL_CONNECTION_LOST:
        LOG_E(TAG, "Connection lost");
        break;
      case WL_DISCONNECTED:
        LOG_E(TAG, "Disconnected");
        break;
      default:
        LOG_E(TAG, "WiFi error code: %d", WiFi.status());
    }
    
    WiFi.disconnect();
    WiFi.mode(WIFI_AP);
    WiFi.softAP(AP_NAME, AP_PASSWORD);
  }
}

void CaptivePortalManager::handleNotFound() {
  server->sendHeader("Location", "http://192.168.4.1/", true);
  server->send(302, "text/plain", "");
}

void CaptivePortalManager::handleConnectionStatus() {
  DynamicJsonDocument statusDoc(256);
  
  if (WiFi.status() == WL_CONNECTED) {
    statusDoc["connected"] = true;
    statusDoc["ip"] = WiFi.localIP().toString();
    statusDoc["disconnectAP"] = true;
    statusDoc["message"] = "Forbundet til WiFi! Du kan nu lukke denne side.";
  } else {
    statusDoc["connected"] = false;
    
    String errorMessage = "";
    switch (WiFi.status()) {
      case WL_NO_SSID_AVAIL:
        errorMessage = "Netværket blev ikke fundet. Kontrollér SSID.";
        break;
      case WL_CONNECT_FAILED:
        errorMessage = "Kunne ikke forbinde. Kontrollér adgangskode.";
        break;
      case WL_CONNECTION_LOST:
        errorMessage = "Forbindelsen blev tabt.";
        break;
      case WL_DISCONNECTED:
        errorMessage = "Forbindelsen afbrudt.";
        break;
      default:
        errorMessage = "Forbindelsesfejl. Status kode: " + String(WiFi.status());
    }
    
    statusDoc["errorMessage"] = errorMessage;
  }
  
  String jsonResponse;
  serializeJson(statusDoc, jsonResponse);
  server->send(200, "application/json", jsonResponse);
}