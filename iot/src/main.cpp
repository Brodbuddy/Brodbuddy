#include <Arduino.h>
#include "components/wifi_manager.h"
#include "components/bme280_sensor.h"
#include "components/tof_sensor.h"
#include "components/mqtt_manager.h"
#include "i2c_utils.h"
#include "config.h"
#include "display/SourdoughDisplay.h"
#include "components/SourdoughMonitor.h"



#include "state_machine.h"
#include "components/sensor_manager.h"
#include "utils/settings.h"
#include "utils/logger.h"

const char* TAG = "Main";






// Global instances
BroadBuddyWiFiManager wifiManager;
BME280Sensor bmeSensor;
ToFSensor tofSensor;
MqttManager mqttManager;

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




















StateMachine stateMachine;
SensorManager sensorManager;
Settings settings;


unsigned long lastStateCheck = 0;
const unsigned long STATE_CHECK_INTERVAL = 100;

void setup()
{
  Serial.begin(115200);
  Logger::begin(LOG_INFO);

  LOG_I(TAG, "--- Sourdough analyzer startup monitoring ---");

  if (!settings.begin()) {
    LOG_E(TAG, "Failed to initialize settings");
    stateMachine.transitionTo(STATE_ERROR);
    return;
  }

  //display.begin();
  data = monitor.generateMockData();
  monitor.updateDisplay(data);

  if (sensorManager.begin()) {
    LOG_E(TAG, "Failed to initialize sensors");
  }

  sensorManager.setCalibration(
    settings.getTempOffset(),
    settings.getHumOffset()
  );
  sensorManager.setReadInterval(settings.getSensorInterval() * 1000);

  wifiManager.setup();

  stateMachine.transitionTo(STATE_CONNECTING_WIFI);





  // delay(2000);
  // Serial.println("\n\n==== BroadBuddy Sensor Platform - Starting up ====");

  // delay(1000);
  // Serial.println("\n\n--- Brodbuddy Sourdough Monitor ---");

  // display.begin();

  // data = monitor.generateMockData();

  // monitor.updateDisplay(data);

  // Serial.println("--- Sourdough monitor ready ---");

  // // Initialisér I2C bus først
  // initI2C();

  // // Scan I2C-bus for enheder
  // scanI2CBus();

  // // Initialize WiFi manager
  // wifiManager.setup();

  // // Initialize MQTT (after WiFi is established)

  // // Initialize sensors - først BME280 da den indeholder I2C init
  // bool bmeConnected = bmeSensor.setup();
  // if (!bmeConnected)
  // {
  //   Serial.println("Warning: BME280 sensor initialization failed");
  // }

  // // Kort pause mellem sensor initialiseringer
  // delay(100);

  // // Derefter ToF sensor på samme I2C bus
  // bool tofConnected = tofSensor.setup();
  // if (!tofConnected)
  // {
  //   Serial.println("Warning: Time of Flight sensor initialization failed");
  // }

  // // Bestem operationstilstand baseret på tilsluttede sensorer
  // if (tofConnected)
  // {
  //   Serial.println("Time of Flight sensor detected - operating in sourdough monitor mode");
  //   sourdoughMonitorMode = true;
  // }
  // else if (bmeConnected)
  // {
  //   Serial.println("BME280 sensor detected - operating in temperature/humidity monitor mode");
  //   sourdoughMonitorMode = false;
  // }
  // else
  // {
  //   Serial.println("ERROR: No sensors detected! System will restart");
  //   delay(3000);
  //   ESP.restart();
  // }

  // Serial.println("Setup completed");

  // // Send an initial measurement after setup
  // delay(5000);
  // if (sourdoughMonitorMode && tofSensor.isConnected())
  // {
  //   // Brug den nye publishToFData metode i stedet for publishSourdoughData
  //   mqttManager.publishToFData(
  //       tofSensor.getDistance(),
  //       tofSensor.getInitialHeight(),
  //       tofSensor.getRiseAmount(),
  //       tofSensor.getRisePercentage(),
  //       tofSensor.getRiseRate());
  //   lastToFPublishTime = millis();
  // }
  // else if (bmeSensor.isConnected())
  // {
  //   // Brug den nye publishBME280Data metode i stedet for publishMeasurement
  //   mqttManager.publishBME280Data(
  //       bmeSensor.getTemperature(),
  //       bmeSensor.getHumidity(),
  //       bmeSensor.getPressure());
  //   lastBMEPublishTime = millis();
  // }

  // stateMachine.transitionTo(STATE_CONNECTING_WIFI);
}

void loop()
{
  unsigned long currentMillis = millis();
  LOG_D(TAG, "currentMillis: " + currentMillis);

  if (currentMillis - lastStateCheck >= STATE_CHECK_INTERVAL) { 
    lastStateCheck = currentMillis;

    switch (stateMachine.getCurrentState()) {
      case STATE_BOOT:
        stateMachine.transitionTo(STATE_CONNECTING_WIFI);
        break;

      case STATE_CONNECTING_WIFI:
          wifiManager.loop();
          if (wifiManager.getStatus() == WIFI_CONNECTED) {
            LOG_I(TAG, "WiFi connected");
            stateMachine.transitionTo(STATE_CONNECTING_MQTT);
          }
          break;

      case STATE_CONNECTING_MQTT:
        if (!mqttManager.isConnected()) {
          String server = settings.getMqttServer();
          int port = settings.getMqttPort();
          String user = settings.getMqttUser();
          String password = settings.getMqttPassword();
          String deviceId = settings.getDeviceId();
          if (mqttManager.begin(server.c_str(), port, user.c_str(), password.c_str(), deviceId.c_str())) {
            stateMachine.transitionTo(STATE_SENSING);
          }
        } else {
          stateMachine.transitionTo(STATE_SENSING);
        }
        break;

      case STATE_SENSING:
        if (sensorManager.shouldRead()) {
          if (sensorManager.readAllSensors()) {
            LOG_I(TAG, "Sensor reading complete");
            stateMachine.transitionTo(STATE_UPDATING_DISPLAY);
          }
        }
        break;

      case STATE_UPDATING_DISPLAY:
        display.updateDisplay();
        LOG_I(TAG, "Display updated");
        stateMachine.transitionTo(STATE_PUBLISHING_DATA);
        break;

      case STATE_PUBLISHING_DATA:
        if (!mqttManager.isConnected()) {
          stateMachine.transitionTo(STATE_CONNECTING_MQTT);
        } else {
          DynamicJsonDocument doc(256);
          SensorData data = sensorManager.getCurrentData();
          doc["temperature"] = data.inTemp;
          doc["humidity"] = data.inHumidity;
          doc["rise"] = data.currentRisePercent;

          if (mqttManager.publish("kakao/maelk", doc)) {
            LOG_I(TAG, "Data published");
          }
          stateMachine.transitionTo(STATE_SLEEP);
        }
        break;

      case STATE_SLEEP:
        if (settings.getLowPowerMode()) {
          WiFi.disconnect();
          esp_sleep_enable_timer_wakeup(settings.getSensorInterval() * 1000000);
          esp_light_sleep_start();
          WiFi.reconnect();
          stateMachine.transitionTo(STATE_CONNECTING_WIFI);
        } else {
          if (stateMachine.shouldTransition(settings.getSensorInterval() * 1000)) {
            stateMachine.transitionTo(STATE_SENSING);
          }
        }
        break;

      case STATE_ERROR:
        LOG_E(TAG, "System in error state");
        delay(5000);
        ESP.restart();
        break;
    }
  }

  mqttManager.loop();
}



//   // Handle WiFi
//   wifiManager.loop();

//   // Handle MQTT
//   mqttManager.loop();

//   // Handle sensors - altid kør begge loop for at undgå timeout
//   bmeSensor.loop();
//   tofSensor.loop();

//   // Check if WiFi reset was requested
//   if (wifiManager.resetRequested())
//   {
//     Serial.println("Reset requested by user - restarting device");
//     delay(500);
//     ESP.restart();if (mqttManager.isConnected())
//   }

//   // Håndter data publishing baseret på mode
//   unsigned long currentMillis = millis();

//   // Sourdough monitor mode (ToF sensor)
//   if (sourdoughMonitorMode && tofSensor.isConnected() && bmeSensor.isConnected() &&
//       tofSensor.isRangeValid() && currentMillis - lastPublishTime >= PUBLISH_INTERVAL)
//   {

//     Serial.println("-------------");
//     Serial.println("Time to publish telemetry data:");
//     mqttManager.printLocalTime();

//     if (wifiManager.getStatus() == WIFI_CONNECTED)
//     {
//       // Brug den nye publishTelemetry metode der kombinerer data fra begge sensorer
//       bool published = mqttManager.publishTelemetry(
//           // ToF data
//           tofSensor.getDistance(),
//           tofSensor.getInitialHeight(),
//           tofSensor.getRiseAmount(),
//           tofSensor.getRisePercentage(),
//           tofSensor.getRiseRate(),
//           // BME280 data
//           bmeSensor.getTemperature(),
//           bmeSensor.getHumidity(),
//           bmeSensor.getPressure());

//       if (published)
//       {
//         lastPublishTime = currentMillis;
//       }
//     }
//     else
//     {
//       Serial.println("Cannot publish: WiFi not available");
//     }
//     Serial.println("-------------");
//   }

//   // BME280 sensor mode
//   else if (bmeSensor.isConnected() && currentMillis - lastBMEPublishTime >= PUBLISH_INTERVAL)
//   {
//     Serial.println("-------------");
//     Serial.println("Time to publish BME280 data:");
//     mqttManager.printLocalTime();

//     if (wifiManager.getStatus() == WIFI_CONNECTED)
//     {
//       // Brug den nye publishBME280Data metode i stedet for publishMeasurement
//       bool published = mqttManager.publishBME280Data(
//           bmeSensor.getTemperature(),
//           bmeSensor.getHumidity(),
//           bmeSensor.getPressure());

//       if (published)
//       {
//         lastBMEPublishTime = currentMillis;
//       }
//     }
//     else
//     {
//       Serial.println("Cannot publish: WiFi not available");
//     }
//     Serial.println("-------------");
//   }

//   // Tjek og vis modtaget data fra MQTT-emner periodisk (hvert 5. sekund)
//   if (currentMillis - lastDataCheckTime >= 5000)
//   {
//     lastDataCheckTime = currentMillis;

//     // Kun tjek modtagne data hvis vi er forbundet til MQTT
//     if (mqttManager.isConnected())
//     {
//       // Hent seneste BME280 data fra MQTT (hvis tilgængelig)
//       JsonVariant bmeData = mqttManager.getLastBME280Data();
//       if (!bmeData.isNull() && bmeData.containsKey("Temperature"))
//       {
//         Serial.println("\n===== Seneste BME280 data modtaget fra MQTT =====");
//         Serial.print("DeviceId: ");
//         if (bmeData.containsKey("DeviceId"))
//         {
//           Serial.println(bmeData["DeviceId"].as<String>());
//         }
//         else
//         {
//           Serial.println("Ikke angivet");
//         }

//         Serial.print("Temperatur: ");
//         Serial.print(bmeData["Temperature"].as<float>(), 1);
//         Serial.println(" °C");

//         Serial.print("Luftfugtighed: ");
//         Serial.print(bmeData["Humidity"].as<float>(), 1);
//         Serial.println(" %");

//         if (bmeData.containsKey("Pressure"))
//         {
//           Serial.print("Tryk: ");
//           Serial.print(bmeData["Pressure"].as<float>(), 1);
//           Serial.println(" hPa");
//         }

//         if (bmeData.containsKey("Timestamp"))
//         {
//           Serial.print("Timestamp: ");
//           Serial.println(bmeData["Timestamp"].as<String>());
//         }
//         Serial.println("=================================================");
//       }

//       // Hent seneste ToF data fra MQTT (hvis tilgængelig)
//       JsonVariant tofData = mqttManager.getLastToFData();
//       if (!tofData.isNull() && tofData.containsKey("Temperature"))
//       { // Temperature bruges som distance
//         Serial.println("\n===== Seneste ToF data modtaget fra MQTT =====");
//         Serial.print("DeviceId: ");
//         if (tofData.containsKey("DeviceId"))
//         {
//           Serial.println(tofData["DeviceId"].as<String>());
//         }
//         else
//         {
//           Serial.println("Ikke angivet");
//         }

//         Serial.print("Distance: ");
//         Serial.print(tofData["Temperature"].as<float>(), 1); // Temperature bruges som distance
//         Serial.println(" mm");

//         Serial.print("Stigning %: ");
//         Serial.print(tofData["Humidity"].as<float>(), 1); // Humidity bruges som rise percentage
//         Serial.println(" %");

//         if (tofData.containsKey("RiseAmount"))
//         {
//           Serial.print("Stigning: ");
//           Serial.print(tofData["RiseAmount"].as<int>());
//           Serial.println(" mm");
//         }

//         if (tofData.containsKey("RiseRate"))
//         {
//           Serial.print("Stigningsrate: ");
//           Serial.print(tofData["RiseRate"].as<float>(), 1);
//           Serial.println(" % per time");
//         }

//         if (tofData.containsKey("Timestamp"))
//         {
//           Serial.print("Timestamp: ");
//           Serial.println(tofData["Timestamp"].as<String>());
//         }
//         Serial.println("=================================================");
//       }
//     }
//   }

//   // Small delay to prevent tight looping
//   delay(10);
// }