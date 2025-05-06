#include <Arduino.h>
#include <Wire.h>
#include <VL53L0X.h>

// Sensor object
VL53L0X sensor;

// Constants
const int MEASUREMENT_INTERVAL_MS = 10000;  // 10 second between measurements
const int STARTUP_DELAY_MS = 2000;          // Delay before initial measurement
const int NUM_SAMPLES = 5;                  // Number of samples to average for each reading
const int XSHUT_PIN = 4;  // XSHUT pin connected to GPIO 4

// Variables
unsigned long lastMeasurementTime = 0;
int initialHeight = 0;
int currentHeight = 0;
float risePercentage = 0.0;
float riseRate = 0.0;        // Rise per hour
unsigned long startTime = 0; // When monitoring began

// Function prototypes
int takeMeasurement();
void printMeasurement(int measurement, int rise, float percentage, float hourlyRate);
void scanI2C();

void setup() {
  // Initialize serial communication
  Serial.begin(115200);
  delay(5000); // Wait 5 seconds to ensure serial connection
  
  Serial.println("\n\n=== Sourdough Rise Monitor ===");
  
  // Reset the sensor using XSHUT
  pinMode(XSHUT_PIN, OUTPUT);
  digitalWrite(XSHUT_PIN, LOW);  // Disable the sensor
  delay(100);
  digitalWrite(XSHUT_PIN, HIGH); // Enable the sensor
  delay(100);
  
  // Initialize I2C communication
  Wire.begin();
  Wire.setClock(50000);  // Set I2C clock to 50kHz (slower and more reliable)
  
  // Scan I2C bus for devices
  Serial.println("Scanning I2C bus for devices...");
  scanI2C();
  
  // Initialize the VL53L0X sensor
  Serial.println("Initializing sensor...");
  if (!sensor.init()) {
    Serial.println("Failed to initialize VL53L0X sensor!");
    
    // Try again with default address
    Serial.println("Trying with default address...");
    sensor.setAddress(0x29);
    if (!sensor.init()) {
      Serial.println("Still failed to initialize. Check wiring!");
      while (1) {
        delay(1000);
      }
    }
  }
  
  // Configure sensor for better accuracy over short distances
  sensor.setTimeout(500);
  
  // Configure for high accuracy mode (longer range but slower)
  sensor.setMeasurementTimingBudget(200000); // 200ms per measurement
  
  sensor.startContinuous();
  
  // Wait for sensor to stabilize
  Serial.println("Sensor initialized. Waiting for stabilization...");
  delay(STARTUP_DELAY_MS);
  
  // Take initial measurement (distance to top of sourdough)
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
  // Check if it's time to take a new measurement
  if (millis() - lastMeasurementTime >= MEASUREMENT_INTERVAL_MS) {
    // Take a new measurement
    currentHeight = takeMeasurement();
    
    if (currentHeight <= 0) {
      Serial.println("Invalid reading. Skipping this measurement.");
    } else {
      // Calculate rise (the difference between initial and current height)
      int rise = initialHeight - currentHeight;
      
      // Calculate percentage rise
      risePercentage = (rise * 100.0) / initialHeight;
      
      // Calculate rise rate per hour
      unsigned long elapsedMinutes = (millis() - startTime) / 60000;
      if (elapsedMinutes > 0) {
        riseRate = (risePercentage * 60.0) / elapsedMinutes; // % per hour
      }
      
      // Print results
      printMeasurement(currentHeight, rise, risePercentage, riseRate);
    }
    
    // Update last measurement time
    lastMeasurementTime = millis();
  }
  
  // Small delay to prevent CPU hogging
  delay(100);
}

// Scan I2C bus for devices
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

// Take multiple measurements and return the average
int takeMeasurement() {
  int sum = 0;
  int validReadings = 0;
  
  for (int i = 0; i < NUM_SAMPLES; i++) {
    int reading = sensor.readRangeContinuousMillimeters();
    if (!sensor.timeoutOccurred() && reading > 0 && reading < 2000) {
      sum += reading;
      validReadings++;
    }
    delay(50); // Short delay between readings
  }
  
  if (validReadings == 0) {
    Serial.println("All sensor readings failed!");
    return -1;
  }
  
  return sum / validReadings;
}

// Print formatted measurement data
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
  Serial.print(percentage, 1); // One decimal place
  Serial.println("%");
  
  Serial.print("Rise rate: ");
  Serial.print(hourlyRate, 1); // One decimal place
  Serial.println("% per hour");
  Serial.println("==============================");
}