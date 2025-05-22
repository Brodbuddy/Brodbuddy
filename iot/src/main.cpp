#include <Arduino.h>
#include "wifi_manager.h"
#include "bme280_sensor.h"
#include "tof_sensor.h"
#include "mqtt_manager.h"
#include "i2c_utils.h"
#include "../include/config.h"
#include "SourdoughDisplay.h"
#include "SourdoughMonitor.h"

// Global instances
BroadBuddyWiFiManager wifiManager;
BME280Sensor bmeSensor;
ToFSensor tofSensor;
MQTTManager mqttManager;

// Timing variables for MQTT publishing
unsigned long lastBMEPublishTime = 0;
unsigned long lastToFPublishTime = 0;
unsigned long lastDataCheckTime = 0; // Ny variabel til at tjekke modtaget data
unsigned long lastPublishTime = 0;

// Mode flag - surdejsmonitor eller temperatur/fugtighed monitor
bool sourdoughMonitorMode = false; // Sættes automatisk baseret på sensorer
SourdoughDisplay display;
SourdoughMonitor monitor(display);
SourdoughData data;

void setup()
{
  Serial.begin(115200);
  delay(2000);
  Serial.println("\n\n==== BroadBuddy Sensor Platform - Starting up ====");

  delay(1000);
  Serial.println("\n\n--- Brodbuddy Sourdough Monitor ---");

  display.begin();

  data = monitor.generateMockData();

  monitor.updateDisplay(data);

  Serial.println("--- Sourdough monitor ready ---");

  // Initialisér I2C bus først
  initI2C();

  // Scan I2C-bus for enheder
  scanI2CBus();

  // Initialize WiFi manager
  wifiManager.setup();

  // Initialize MQTT (after WiFi is established)
  mqttManager.setup();

  // Initialize sensors - først BME280 da den indeholder I2C init
  bool bmeConnected = bmeSensor.setup();
  if (!bmeConnected)
  {
    Serial.println("Warning: BME280 sensor initialization failed");
  }

  // Kort pause mellem sensor initialiseringer
  delay(100);

  // Derefter ToF sensor på samme I2C bus
  bool tofConnected = tofSensor.setup();
  if (!tofConnected)
  {
    Serial.println("Warning: Time of Flight sensor initialization failed");
  }

  // Bestem operationstilstand baseret på tilsluttede sensorer
  if (tofConnected)
  {
    Serial.println("Time of Flight sensor detected - operating in sourdough monitor mode");
    sourdoughMonitorMode = true;
  }
  else if (bmeConnected)
  {
    Serial.println("BME280 sensor detected - operating in temperature/humidity monitor mode");
    sourdoughMonitorMode = false;
  }
  else
  {
    Serial.println("ERROR: No sensors detected! System will restart");
    delay(3000);
    ESP.restart();
  }

  Serial.println("Setup completed");

  // Send an initial measurement after setup
  delay(5000);
  if (sourdoughMonitorMode && tofSensor.isConnected())
  {
    // Brug den nye publishToFData metode i stedet for publishSourdoughData
    mqttManager.publishToFData(
        tofSensor.getDistance(),
        tofSensor.getInitialHeight(),
        tofSensor.getRiseAmount(),
        tofSensor.getRisePercentage(),
        tofSensor.getRiseRate());
    lastToFPublishTime = millis();
  }
  else if (bmeSensor.isConnected())
  {
    // Brug den nye publishBME280Data metode i stedet for publishMeasurement
    mqttManager.publishBME280Data(
        bmeSensor.getTemperature(),
        bmeSensor.getHumidity(),
        bmeSensor.getPressure());
    lastBMEPublishTime = millis();
  }
}

void loop()
{
  // Handle WiFi
  wifiManager.loop();

  // Handle MQTT
  mqttManager.loop();

  // Handle sensors - altid kør begge loop for at undgå timeout
  bmeSensor.loop();
  tofSensor.loop();

  // Check if WiFi reset was requested
  if (wifiManager.resetRequested())
  {
    Serial.println("Reset requested by user - restarting device");
    delay(500);
    ESP.restart();
  }

  // Håndter data publishing baseret på mode
  unsigned long currentMillis = millis();

  // Sourdough monitor mode (ToF sensor)
  if (sourdoughMonitorMode && tofSensor.isConnected() && bmeSensor.isConnected() &&
      tofSensor.isRangeValid() && currentMillis - lastPublishTime >= PUBLISH_INTERVAL)
  {

    Serial.println("-------------");
    Serial.println("Time to publish telemetry data:");
    mqttManager.printLocalTime();

    if (wifiManager.getStatus() == WIFI_CONNECTED)
    {
      // Brug den nye publishTelemetry metode der kombinerer data fra begge sensorer
      bool published = mqttManager.publishTelemetry(
          // ToF data
          tofSensor.getDistance(),
          tofSensor.getInitialHeight(),
          tofSensor.getRiseAmount(),
          tofSensor.getRisePercentage(),
          tofSensor.getRiseRate(),
          // BME280 data
          bmeSensor.getTemperature(),
          bmeSensor.getHumidity(),
          bmeSensor.getPressure());

      if (published)
      {
        lastPublishTime = currentMillis;
      }
    }
    else
    {
      Serial.println("Cannot publish: WiFi not available");
    }
    Serial.println("-------------");
  }

  // BME280 sensor mode
  else if (bmeSensor.isConnected() && currentMillis - lastBMEPublishTime >= PUBLISH_INTERVAL)
  {
    Serial.println("-------------");
    Serial.println("Time to publish BME280 data:");
    mqttManager.printLocalTime();

    if (wifiManager.getStatus() == WIFI_CONNECTED)
    {
      // Brug den nye publishBME280Data metode i stedet for publishMeasurement
      bool published = mqttManager.publishBME280Data(
          bmeSensor.getTemperature(),
          bmeSensor.getHumidity(),
          bmeSensor.getPressure());

      if (published)
      {
        lastBMEPublishTime = currentMillis;
      }
    }
    else
    {
      Serial.println("Cannot publish: WiFi not available");
    }
    Serial.println("-------------");
  }

  // Tjek og vis modtaget data fra MQTT-emner periodisk (hvert 5. sekund)
  if (currentMillis - lastDataCheckTime >= 5000)
  {
    lastDataCheckTime = currentMillis;

    // Kun tjek modtagne data hvis vi er forbundet til MQTT
    if (mqttManager.isConnected())
    {
      // Hent seneste BME280 data fra MQTT (hvis tilgængelig)
      JsonVariant bmeData = mqttManager.getLastBME280Data();
      if (!bmeData.isNull() && bmeData.containsKey("Temperature"))
      {
        Serial.println("\n===== Seneste BME280 data modtaget fra MQTT =====");
        Serial.print("DeviceId: ");
        if (bmeData.containsKey("DeviceId"))
        {
          Serial.println(bmeData["DeviceId"].as<String>());
        }
        else
        {
          Serial.println("Ikke angivet");
        }

        Serial.print("Temperatur: ");
        Serial.print(bmeData["Temperature"].as<float>(), 1);
        Serial.println(" °C");

        Serial.print("Luftfugtighed: ");
        Serial.print(bmeData["Humidity"].as<float>(), 1);
        Serial.println(" %");

        if (bmeData.containsKey("Pressure"))
        {
          Serial.print("Tryk: ");
          Serial.print(bmeData["Pressure"].as<float>(), 1);
          Serial.println(" hPa");
        }

        if (bmeData.containsKey("Timestamp"))
        {
          Serial.print("Timestamp: ");
          Serial.println(bmeData["Timestamp"].as<String>());
        }
        Serial.println("=================================================");
      }

      // Hent seneste ToF data fra MQTT (hvis tilgængelig)
      JsonVariant tofData = mqttManager.getLastToFData();
      if (!tofData.isNull() && tofData.containsKey("Temperature"))
      { // Temperature bruges som distance
        Serial.println("\n===== Seneste ToF data modtaget fra MQTT =====");
        Serial.print("DeviceId: ");
        if (tofData.containsKey("DeviceId"))
        {
          Serial.println(tofData["DeviceId"].as<String>());
        }
        else
        {
          Serial.println("Ikke angivet");
        }

        Serial.print("Distance: ");
        Serial.print(tofData["Temperature"].as<float>(), 1); // Temperature bruges som distance
        Serial.println(" mm");

        Serial.print("Stigning %: ");
        Serial.print(tofData["Humidity"].as<float>(), 1); // Humidity bruges som rise percentage
        Serial.println(" %");

        if (tofData.containsKey("RiseAmount"))
        {
          Serial.print("Stigning: ");
          Serial.print(tofData["RiseAmount"].as<int>());
          Serial.println(" mm");
        }

        if (tofData.containsKey("RiseRate"))
        {
          Serial.print("Stigningsrate: ");
          Serial.print(tofData["RiseRate"].as<float>(), 1);
          Serial.println(" % per time");
        }

        if (tofData.containsKey("Timestamp"))
        {
          Serial.print("Timestamp: ");
          Serial.println(tofData["Timestamp"].as<String>());
        }
        Serial.println("=================================================");
      }
    }
  }

  // Small delay to prevent tight looping
  delay(10);
}