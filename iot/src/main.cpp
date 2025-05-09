#include <Arduino.h>
#include <Wire.h>
#include <VL53L0X.h>
#include <WiFiManager.h>
#include <PubSubClient.h> 
#include <WiFiClientSecure.h> 
#include <ArduinoJson.h> 

// Sensor object
VL53L0X sensor;

// MQTT Broker settings
const char* mqtt_server = "15b24181a314453a912a712175056638.s1.eu.hivemq.cloud";
const int mqtt_port = 8883; 
const char* mqtt_user = "thom625b"; 
const char* mqtt_password = "Kakao1!!"; 
const char* device_id = "sourdough_monitor_01"; 
const char* mqtt_topic = "devices/sourdough_monitor_01/telemetry"; 

// Constants
const int MEASUREMENT_INTERVAL_MS = 600000;  // 10 minutter mellem hver måling
const int STARTUP_DELAY_MS = 2000;          // Forsinkelse før første måling
const int NUM_SAMPLES = 5;                  // Antal prøver til gennemsnit for hver aflæsning
const int XSHUT_PIN = 4;  

// Variables
unsigned long lastMeasurementTime = 0;
int initialHeight = 0;
int currentHeight = 0;
float risePercentage = 0.0;
float riseRate = 0.0;        // hævning pr. time
unsigned long startTime = 0; 

// WiFi og MQTT klienter
WiFiClientSecure espClient;
PubSubClient mqttClient(espClient);

// Funktionsprototyper
int takeMeasurement();
void printMeasurement(int measurement, int rise, float percentage, float hourlyRate);
void scanI2C();
void reconnectMqtt();
void publishMeasurement(int measurement, int rise, float percentage, float hourlyRate);

void setup() {
  Serial.begin(115200);
  Serial.println("WiFiManager Eksempel");

  delay(5000); 
  
  WiFiManager wifiManager;
  wifiManager.setConfigPortalTimeout(180);

  if(!wifiManager.autoConnect("ESP32_AP")) {
    Serial.println("Failed to connect and hit timeout");
    ESP.restart(); 
    delay(1000);
  }
   
  Serial.println("Connected to WiFi!");
  Serial.print("IP address: ");
  Serial.println(WiFi.localIP());

  espClient.setInsecure();
  
  mqttClient.setServer(mqtt_server, mqtt_port);

  Serial.println("\n\n=== Sourdough Rise Monitor ===");
  
  pinMode(XSHUT_PIN, OUTPUT);
  digitalWrite(XSHUT_PIN, LOW);  // Deaktiver sensoren
  delay(100);
  digitalWrite(XSHUT_PIN, HIGH); // Aktiver sensoren
  delay(100);
  
  Wire.begin();
  Wire.setClock(50000);  
  
  // Scan I2C bus for devices
  Serial.println("Scanning I2C bus for devices...");
  scanI2C();
  
  // Initalisere VL53L0X sensoren
  Serial.println("Initializing sensor...");
  if (!sensor.init()) {
    Serial.println("Failed to initialize VL53L0X sensor!");
    
    // Prøver igen med default addressen
    Serial.println("Trying with default address...");
    sensor.setAddress(0x29);
    if (!sensor.init()) {
      Serial.println("Still failed to initialize. Check wiring!");
      while (1) {
        delay(1000);
      }
    }
  }
  
  sensor.setTimeout(500);
  
  sensor.setMeasurementTimingBudget(200000); // 200ms per måling
  
  sensor.startContinuous();
  
  
  Serial.println("Sensor initialized. Waiting for stabilization...");
  delay(STARTUP_DELAY_MS);
  
  Serial.println("Taking initial measurement...");
  initialHeight = takeMeasurement();
  
  Serial.print("Initial distance to dough: ");
  Serial.print(initialHeight);
  Serial.println(" mm");
  
  if (initialHeight <= 0) {
    Serial.println("Invalid initial reading! Please restart.");
    while (1) {
      delay(1000);
    }
  }
  
  startTime = millis();
  Serial.println("Monitoring started. Will take measurements every 10 seconds.");
  Serial.println("==============================");
}

void loop() {
  if (!mqttClient.connected()) {
    reconnectMqtt();
  }
  mqttClient.loop();
  
  // Tjekker om det er tid til at lave en ny måling
  if (millis() - lastMeasurementTime >= MEASUREMENT_INTERVAL_MS) {
    // Laver en ny måling
    currentHeight = takeMeasurement();
    
    if (currentHeight <= 0) {
      Serial.println("Invalid reading. Skipping this measurement.");
    } else {
      int rise = initialHeight - currentHeight;
      
      risePercentage = (rise * 100.0) / initialHeight;
      
      unsigned long elapsedMinutes = (millis() - startTime) / 60000;
      if (elapsedMinutes > 0) {
        riseRate = (risePercentage * 60.0) / elapsedMinutes;
      }
      
      printMeasurement(currentHeight, rise, risePercentage, riseRate);
      
      // Publish til MQTT
      publishMeasurement(currentHeight, rise, risePercentage, riseRate);
    }
    
    // Opdater sidste måletidspunkt
    lastMeasurementTime = millis();
  }
  
  delay(100);
}

void reconnectMqtt() {
  int retries = 0;
  while (!mqttClient.connected() && retries < 3) {
    Serial.print("Attempting MQTT connection...");
    
    if (mqttClient.connect(device_id, mqtt_user, mqtt_password)) {
      Serial.println("connected");
      break;
    } else {
      Serial.print("failed, rc=");
      Serial.print(mqttClient.state());
      Serial.println(" retrying in 5 seconds");
      retries++;
      delay(5000);
    }
  }
}

  // Publish måledatat til MQTT
void publishMeasurement(int measurement, int rise, float percentage, float hourlyRate) {
  DynamicJsonDocument doc(256);
  
  // Tilføj data, så de matcher C# DeviceTelemetry-poststrukturen
  doc["DeviceId"] = device_id;
  doc["Temperature"] = measurement; 
  doc["Humidity"] = risePercentage; 
  doc["Timestamp"] = millis();     
  
  // Serialiser til JSON
  char buffer[256];
  serializeJson(doc, buffer);
  
  // Publish til MQTT topic
  if (mqttClient.publish(mqtt_topic, buffer)) {
    Serial.println("MQTT message published successfully");
  } else {
    Serial.println("Failed to publish MQTT message");
  }
}

void scanI2C() {
  byte error, address;
  int deviceCount = 0;
  
  for(address = 1; address < 127; address++) {
    Wire.beginTransmission(address);
    error = Wire.endTransmission();
    if (error == 0) {
      Serial.print("I2C device found at address 0x");
      if (address < 16) {
        Serial.print("0");
      }
      Serial.print(address, HEX);
      Serial.println();
      deviceCount++;
    }
  }
  
  if (deviceCount == 0) {
    Serial.println("No I2C devices found! Check your wiring.");
  } else {
    Serial.print("Found ");
    Serial.print(deviceCount);
    Serial.println(" device(s)");
  }
}

// Foretag flere målinger og returner gennemsnittet (uændret)
int takeMeasurement() {
  int sum = 0;
  int validReadings = 0;
  
  for (int i = 0; i < NUM_SAMPLES; i++) {
    int reading = sensor.readRangeContinuousMillimeters();
    if (!sensor.timeoutOccurred() && reading > 0 && reading < 2000) {
      sum += reading;
      validReadings++;
    }
    delay(50); 
  }
  
  if (validReadings == 0) {
    Serial.println("All sensor readings failed!");
    return -1;
  }
  
  return sum / validReadings;
}

void printMeasurement(int measurement, int rise, float percentage, float hourlyRate) {
  unsigned long elapsedMinutes = (millis() - startTime) / 60000;
  unsigned long hours = elapsedMinutes / 60;
  unsigned long mins = elapsedMinutes % 60;
  
  Serial.println("==============================");
  Serial.print("Time elapsed: ");
  if (hours > 0) {
    Serial.print(hours);
    Serial.print("h ");
  }
  Serial.print(mins);
  Serial.println("m");
  
  Serial.print("Current distance: ");
  Serial.print(measurement);
  Serial.println(" mm");
  
  Serial.print("Rise amount: ");
  Serial.print(rise);
  Serial.println(" mm");
  
  Serial.print("Rise percentage: ");
  Serial.print(percentage, 1); 
  Serial.println("%");
  
  Serial.print("Rise rate: ");
  Serial.print(hourlyRate, 1);
  Serial.println("% per hour");
  Serial.println("==============================");
}