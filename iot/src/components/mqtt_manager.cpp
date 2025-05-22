// mqtt_manager.cpp
#include "components/mqtt_manager.h"
#include "utils/logger.h"
#include "config.h"

static const char* TAG = "MqttManager";

MqttManager::MqttManager() : mqttClient(_wifiClient),
                             lastReconnectAttempt(0),
                             ntpConfigured(false)
{
}

bool MqttManager::begin(const char* server, int port, const char* user, const char* password, const char* clientId) {
    _server = server;
    _port = port;
    _user = user;
    _password = password;
    _clientId = clientId;

    mqttClient.setServer(server, port);
    mqttClient.setCallback([this](char *topic, byte *payload, unsigned int length)
                           { this->processMessage(topic, payload, length); });
    
    return reconnect();
}

bool MqttManager::setup()
{
    // Opsæt NTP
    configureNTP();

    // Opsæt MQTT
    espClient.setInsecure(); // Deaktiver certifikatvalidering (skift til certifikat i produktion)
    mqttClient.setServer(MQTT_SERVER, MQTT_PORT);
    mqttClient.setKeepAlive(60);     // Hold forbindelsen i live i 60 sekunder
    mqttClient.setSocketTimeout(30); // Socket timeout på 30 sekunder
    mqttClient.setBufferSize(512);   // Forøg buffer størrelse for større beskeder

    // Registrer callback funktion for indkommende meddelelser
    mqttClient.setCallback([this](char *topic, byte *payload, unsigned int length)
                           { this->processMessage(topic, payload, length); });

    // Initialiser tilfældig seed til klient-ID generering
    randomSeed(micros());

    // Forsøg at oprette forbindelse til MQTT
    bool connected = reconnect();
    return connected;
}

void MqttManager::loop()
{
    // Check MQTT forbindelse
    if (!mqttClient.connected())
    {
        unsigned long currentMillis = millis();
        if (currentMillis - lastReconnectAttempt > 5000)
        {
            lastReconnectAttempt = currentMillis;
            // Forsøg at genforbinde
            bool reconnected = reconnect();
            if (reconnected)
            {
                lastReconnectAttempt = 0;
            }
        }
    }
    else
    {
        // Klient er tilsluttet
        mqttClient.loop();
    }
}

bool MqttManager::publish(const char* topic, const JsonDocument& data) {
    String json;
    serializeJson(data, json);
    return publish(topic, json.c_str());
}

bool MqttManager::publish(const char* topic, const char* payload) {
    if (!mqttClient.connected()) {
        return false;
    }

    return mqttClient.publish(topic, payload);
}

void MqttManager::processMessage(char *topic, byte *payload, unsigned int length)
{
    // Konverter payload til null-termineret streng
    char message[length + 1];
    memcpy(message, payload, length);
    message[length] = '\0';

    Serial.print("Modtog besked på emne: ");
    Serial.println(topic);
    Serial.print("Besked: ");
    Serial.println(message);

    // Analyser JSON-beskeden
    DynamicJsonDocument doc(512);
    DeserializationError error = deserializeJson(doc, message);

    if (error)
    {
        Serial.print("deserializeJson() fejlede: ");
        Serial.println(error.c_str());
        return;
    }

    // Gem dataen baseret på emnet
    if (strcmp(topic, MQTT_TOPIC_BME280) == 0)
    {
        lastBME280Data = doc;
        Serial.println("BME280 data opdateret");

        // Print nogle værdier for at bekræfte
        if (doc.containsKey("Temperature"))
        {
            Serial.print("Temperatur: ");
            Serial.println(doc["Temperature"].as<float>());
        }
        if (doc.containsKey("Humidity"))
        {
            Serial.print("Luftfugtighed: ");
            Serial.println(doc["Humidity"].as<float>());
        }
    }
    else if (strcmp(topic, MQTT_TOPIC_TOF) == 0)
    {
        lastToFData = doc;
        Serial.println("ToF data opdateret");

        // Print nogle værdier for at bekræfte
        if (doc.containsKey("Temperature"))
        { // I dette tilfælde bruges Temperature til distance
            Serial.print("Distance: ");
            Serial.println(doc["Temperature"].as<float>());
        }
        if (doc.containsKey("RisePercentage"))
        {
            Serial.print("Stigning %: ");
            Serial.println(doc["RisePercentage"].as<float>());
        }
    }
}

JsonVariant MqttManager::getLastBME280Data()
{
    return lastBME280Data.as<JsonVariant>();
}

JsonVariant MqttManager::getLastToFData()
{
    return lastToFData.as<JsonVariant>();
}

bool MqttManager::isConnected()
{
    return mqttClient.connected();
}

void MqttManager::configureNTP()
{
    // Konfigurer tid med NTP
    configTime(GMT_OFFSET_SEC, DAYLIGHT_OFFSET_SEC, NTP_SERVER);
    Serial.println("Synkroniserer tid med NTP...");

    // Vent på tidssynkronisering med timeout
    struct tm timeinfo;
    int retries = 0;
    while (!getLocalTime(&timeinfo) && retries < 10)
    {
        Serial.println("Venter på NTP-tid...");
        delay(500);
        retries++;
    }

    if (retries < 10)
    {
        char timeStringBuf[30];
        strftime(timeStringBuf, sizeof(timeStringBuf), "%Y-%m-%d %H:%M:%S", &timeinfo);
        Serial.print("NTP-tid synkroniseret: ");
        Serial.println(timeStringBuf);
        ntpConfigured = true;
    }
    else
    {
        Serial.println("Kunne ikke synkronisere NTP-tid. Fortsætter uden præcis tid.");
        ntpConfigured = false;
    }
}

bool MqttManager::reconnect()
{
    if (mqttClient.connected()) {
        return true;
    }

    if (_server.length() == 0) {
        return false;
    }

    LOG_I(TAG, "Attempting MQTT connection to %s:%d", _server.c_str(), _port);

    if (mqttClient.connect(_clientId.c_str(), _user.c_str(), _password.c_str())) {
        LOG_I(TAG, "MQTT connected");
        return true;
    } else {
        LOG_E(TAG, "MQTT connection failed, state: %d", mqttClient.state());
        return false;
    }
    // int retries = 0;
    // while (!mqttClient.connected() && retries < 3)
    // {
    //     Serial.print("Forsøger at oprette MQTT-forbindelse til ");
    //     Serial.print(MQTT_SERVER);
    //     Serial.print(":");
    //     Serial.print(MQTT_PORT);
    //     Serial.print("...");

    //     // Opret et klient-ID med tilfældigt suffiks for bedre forbindelsespålidelighed
    //     String clientId = DEVICE_ID;
    //     clientId += "-";
    //     clientId += String(random(0xffff), HEX);

    //     if (mqttClient.connect(clientId.c_str(), MQTT_USER, MQTT_PASSWORD))
    //     {
    //         Serial.println("forbundet");

    //         // Abonner på alle sensortopics
    //         mqttClient.subscribe(MQTT_TOPIC_BME280);
    //         mqttClient.subscribe(MQTT_TOPIC_TOF);

    //         Serial.println("Abonnerer på følgende emner:");
    //         Serial.println(MQTT_TOPIC_BME280);
    //         Serial.println(MQTT_TOPIC_TOF);

    //         // Publicer en forbindelsesstatusmeddelelse
    //         DynamicJsonDocument statusDoc(128);
    //         statusDoc["device_id"] = DEVICE_ID;
    //         statusDoc["status"] = "connected";
    //         statusDoc["ipAddress"] = WiFi.localIP().toString();
    //         statusDoc["rssi"] = WiFi.RSSI();

    //         char statusBuffer[128];
    //         serializeJson(statusDoc, statusBuffer);

    //         bool statusPublished = mqttClient.publish(MQTT_STATUS_TOPIC, statusBuffer, true);
    //         if (statusPublished)
    //         {
    //             Serial.println("Status-besked publiceret:");
    //             Serial.println(statusBuffer);
    //         }
    //         else
    //         {
    //             Serial.println("Kunne ikke publicere status-besked!");
    //         }

    //         return true;
    //     }
    //     else
    //     {
    //         Serial.print("mislykkedes, rc=");
    //         Serial.print(mqttClient.state());
    //         Serial.println(" prøver igen om 5 sekunder");
    //         retries++;
    //         delay(5000);
    //     }
    // }

    // return false;
}

void MqttManager::printLocalTime()
{
    struct tm timeinfo;
    if (!getLocalTime(&timeinfo))
    {
        Serial.println("Kunne ikke hente tid");
        return;
    }

    char timeStringBuf[30];
    strftime(timeStringBuf, sizeof(timeStringBuf), "%Y-%m-%d %H:%M:%S", &timeinfo);
    Serial.print("Aktuel tid: ");
    Serial.println(timeStringBuf);
}

bool MqttManager::getFormattedTime(char *buffer, size_t bufferSize)
{
    struct tm timeinfo;
    if (getLocalTime(&timeinfo))
    {
        // Format: YYYY-MM-DDThh:mm:ss (ISO 8601 uden tidszone)
        strftime(buffer, bufferSize, "%Y-%m-%dT%H:%M:%S", &timeinfo);
        return true;
    }
    return false;
}

// BME280 sensor data publicering
bool MqttManager::publishBME280Data(float temperature, float humidity, float pressure)
{
    DynamicJsonDocument doc(256);

    // Feltnavne der matcher DeviceTelemetry-modellen i C#
    doc["DeviceId"] = DEVICE_ID;
    doc["Temperature"] = temperature; // Celsius
    doc["Humidity"] = humidity;       // Procent

    // Tilføj timestamp i ISO 8601 format
    char timeString[30];
    bool timeAvailable = getFormattedTime(timeString, sizeof(timeString));

    if (timeAvailable)
    {
        doc["Timestamp"] = timeString;
    }
    else
    {
        doc["Timestamp"] = millis();
        Serial.println("Advarsel: Kunne ikke få NTP-tid, bruger millis() som timestamp");
    }

    // Tilføj ekstra metadata
    doc["Pressure"] = pressure; // hPa
    doc["SensorType"] = "BME280";

    // Serialiser til JSON
    char buffer[256];
    serializeJson(doc, buffer);

    // Publicer til BME280-specifikt topic
    Serial.print("Forsøger at publicere BME280 data til MQTT: ");
    Serial.println(MQTT_TOPIC_BME280);

    // Publicer med retained flag for persistens
    bool success = mqttClient.publish(MQTT_TOPIC_BME280, buffer, true);

    if (success)
    {
        Serial.println("===== BME280 data publiceret med succes =====");
        Serial.print("Topic: ");
        Serial.println(MQTT_TOPIC_BME280);
        Serial.print("Payload: ");
        Serial.println(buffer);
        Serial.println("=============================================");
    }
    else
    {
        Serial.println("FEJL: Kunne ikke publicere BME280 data");
        Serial.print("MQTT-tilstand: ");
        Serial.println(mqttClient.state());

        // Prøv igen efter genopkobling
        bool reconnected = reconnect();
        if (reconnected)
        {
            bool retrySuccess = mqttClient.publish(MQTT_TOPIC_BME280, buffer, true);
            if (retrySuccess)
            {
                Serial.println("===== BME280 data publiceret ved andet forsøg =====");
                return true;
            }
            else
            {
                Serial.println("FEJL: Kunne stadig ikke publicere BME280 data");
            }
        }
    }

    return success;
}

// Time of Flight sensor data
bool MqttManager::publishToFData(int currentDistance, int initialHeight, int riseAmount,
                                 float risePercentage, float riseRate)
{
    DynamicJsonDocument doc(256);

    // Feltnavne der matcher data format
    doc["DeviceId"] = DEVICE_ID;
    doc["distance"] = (double)currentDistance; // Distance i mm
    doc["risePercentage"] = risePercentage;    // RisePercentage i %

    // Tilføj timestamp i ISO 8601 format
    char timeString[30];
    bool timeAvailable = getFormattedTime(timeString, sizeof(timeString));

    if (timeAvailable)
    {
        doc["Timestamp"] = timeString;
    }
    else
    {
        doc["Timestamp"] = millis();
        Serial.println("Advarsel: Kunne ikke få NTP-tid, bruger millis() som timestamp");
    }

    // Tilføj ekstra metadata
    doc["InitialHeight"] = initialHeight;
    doc["RiseAmount"] = riseAmount;
    doc["RiseRate"] = riseRate;
    doc["SensorType"] = "ToF";

    // Serialiser til JSON
    char buffer[256];
    serializeJson(doc, buffer);

    // Publicer til ToF-specifikt topic
    Serial.print("Forsøger at publicere ToF data til MQTT: ");
    Serial.println(MQTT_TOPIC_TOF);

    // Publicer med retained flag for persistens
    bool success = mqttClient.publish(MQTT_TOPIC_TOF, buffer, true);

    if (success)
    {
        Serial.println("===== ToF data publiceret med succes =====");
        Serial.print("Topic: ");
        Serial.println(MQTT_TOPIC_TOF);
        Serial.print("Payload: ");
        Serial.println(buffer);
        Serial.println("=============================================");
    }
    else
    {
        Serial.println("FEJL: Kunne ikke publicere ToF data");
        Serial.print("MQTT-tilstand: ");
        Serial.println(mqttClient.state());

        // Prøv igen efter genopkobling
        bool reconnected = reconnect();
        if (reconnected)
        {
            bool retrySuccess = mqttClient.publish(MQTT_TOPIC_TOF, buffer, true);
            if (retrySuccess)
            {
                Serial.println("===== ToF data publiceret ved andet forsøg =====");
                return true;
            }
            else
            {
                Serial.println("FEJL: Kunne stadig ikke publicere ToF data");
            }
        }
    }

    return success;
}

// Samlet telemetri med data fra alle sensorer
bool MqttManager::publishTelemetry(
    int currentDistance, int initialHeight, int riseAmount, float risePercentage, float riseRate,
    float temperature, float humidity, float pressure)
{

    DynamicJsonDocument doc(512);

    // Flad struktur med alle sensordata
    doc["DeviceId"] = DEVICE_ID;
    doc["Temperature"] = temperature;
    doc["Humidity"] = humidity;
    doc["Pressure"] = pressure;
    doc["Distance"] = (double)currentDistance;
    doc["InitialHeight"] = initialHeight;
    doc["RiseAmount"] = riseAmount;
    doc["RisePercentage"] = risePercentage;
    doc["RiseRate"] = riseRate;
    doc["SensorType"] = "Combined";

    // Timestamp
    char timeString[30];
    bool timeAvailable = getFormattedTime(timeString, sizeof(timeString));

    if (timeAvailable)
    {
        doc["Timestamp"] = timeString;
    }
    else
    {
        doc["Timestamp"] = millis();
        Serial.println("Advarsel: Kunne ikke få NTP-tid, bruger millis() som timestamp");
    }

    // Gruppér sensor data
    JsonObject tofData = doc.createNestedObject("tof");
    tofData["distance"] = (double)currentDistance;
    tofData["initialHeight"] = initialHeight;
    tofData["riseAmount"] = riseAmount;
    tofData["risePercentage"] = risePercentage;
    tofData["riseRate"] = riseRate;

    JsonObject envData = doc.createNestedObject("bme280");
    envData["temperature"] = temperature;
    envData["humidity"] = humidity;
    envData["pressure"] = pressure;

    // Serialiser til JSON
    char buffer[512];
    serializeJson(doc, buffer);

    // Publicer til telemetry-emne
    Serial.print("Forsøger at publicere samlet telemetri til MQTT: ");
    Serial.println(MQTT_TOPIC_TELEMETRY);

    // Publicer med retained flag
    bool success = mqttClient.publish(MQTT_TOPIC_TELEMETRY, buffer, true);

    if (success)
    {
        Serial.println("===== Telemetri data publiceret med succes =====");
    }
    else
    {
        Serial.println("FEJL: Kunne ikke publicere telemetri data");

        // Prøv igen efter genopkobling
        bool reconnected = reconnect();
        if (reconnected)
        {
            bool retrySuccess = mqttClient.publish(MQTT_TOPIC_TELEMETRY, buffer, true);
            if (retrySuccess)
            {
                Serial.println("===== Telemetri data publiceret ved andet forsøg =====");
                return true;
            }
        }
    }

    return success;
}