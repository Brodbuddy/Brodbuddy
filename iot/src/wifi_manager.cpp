#include "wifi_manager.h"
#include "../include/config.h"

TaskHandle_t blinkTaskHandle = NULL;

BroadBuddyWiFiManager::BroadBuddyWiFiManager() : currentStatus(WIFI_DISCONNECTED),
                                                 connectStartTime(0),
                                                 previousMillis(0),
                                                 ledState(LOW),
                                                 lastWiFiCheck(0),
                                                 buttonPressed(false),
                                                 buttonPressStart(0),
                                                 resetRequest(false)
{
}

void BroadBuddyWiFiManager::setup()
{
    pinMode(LED_PIN, OUTPUT);
    pinMode(BUTTON_PIN, INPUT_PULLUP);

    // Check for reset button - hvis holdt nede ved start
    if (digitalRead(BUTTON_PIN) == LOW)
    {
        Serial.println("Reset button pressed during startup - clearing WiFi settings");
        wifiManager.resetSettings();
        delay(1000);
        ESP.restart();
    }

    currentStatus = WIFI_CONNECTING;
    ledState = LOW;
    digitalWrite(LED_PIN, ledState);
    connectStartTime = millis();

    // WiFiManager grundlæggende konfiguration
    wifiManager.setDebugOutput(true);
    wifiManager.setConnectTimeout(30);
    wifiManager.setConnectRetries(1);

    // Sæt hostname for mDNS
    wifiManager.setHostname(HOSTNAME);

    // Konfigurer exit håndtering
    wifiManager.setConfigPortalTimeout(0); // Deaktiver timeout
    wifiManager.setBreakAfterConfig(true); // Exit efter konfiguration

    // Skjul unødvendige felter
    wifiManager.setShowInfoErase(false);
    wifiManager.setShowInfoUpdate(false);
    wifiManager.setShowStaticFields(false);
    wifiManager.setShowDnsFields(false);
    wifiManager.setRemoveDuplicateAPs(true);

    // Simpel tilpasning af portal design
    wifiManager.setCustomHeadElement("<style>"
                                     ".q{margin-bottom:10px;padding:10px;border-radius:8px;background-color:#f8f8f8;}"
                                     ".q:hover{background-color:#e6f2ff;cursor:pointer;}"
                                     "button{background-color:#0066cc;color:white;border:none;padding:10px;border-radius:4px;cursor:pointer;}"
                                     "small{display:none;}" // Skjul small tekst (styrke procent)
                                     "h1{color:#0066cc;}"
                                     ".c{display:none;}" // Skjul unødvendige felter
                                     "</style>");

    // AP callback
    wifiManager.setAPCallback([](WiFiManager *myWiFiManager)
                              {
    Serial.println("Entering config mode");
    Serial.println("Connect to WiFi: " + String(AP_NAME));
    Serial.println("Then go to: http://192.168.4.1"); });

    wifiManager.setSaveConfigCallback([]()
                                      { Serial.println("Config saved - connection successful!"); });

    // Start blink task
    createBlinkTask();

    // Start config portal med simpelt interface
    Serial.println("Starting config portal...");

    // Kør portal i et loop - hvis exit trykkes, scanner og genstarter den
    do
    {
        // Scan efter netværk, så listen er frisk hver gang
        Serial.println("Scanning for networks...");
        int n = WiFi.scanNetworks();
        Serial.print("Found ");
        Serial.print(n);
        Serial.println(" networks");

        // Hvis portal afsluttes uden forbindelse, kommer vi herind igen
        if (!wifiManager.startConfigPortal(AP_NAME, AP_PASSWORD))
        {
            Serial.println("Portal exited - rescanning networks and restarting");
            delay(500);
            // Loop fortsætter
        }
        else
        {
            // Hvis vi kommer hertil, er der forbundet
            Serial.println("Connected to WiFi!");
            Serial.print("IP address: ");
            Serial.println(WiFi.localIP());
            currentStatus = WIFI_CONNECTED;
            digitalWrite(LED_PIN, HIGH);
            break; // Afslut loop ved forbindelse
        }
    } while (true);
}

void BroadBuddyWiFiManager::loop()
{
    // LED handling
    if (currentStatus == WIFI_CONNECTED)
    {
        digitalWrite(LED_PIN, HIGH);
    }
    else if (currentStatus == WIFI_CONNECTING && (millis() - connectStartTime < 30000))
    {
        // Backup blinking (hvis task'en skulle fejle)
        unsigned long currentMillis = millis();
        if (currentMillis - previousMillis >= LED_BLINK_NORMAL)
        {
            previousMillis = currentMillis;
            ledState = !ledState;
            digitalWrite(LED_PIN, ledState);
        }
    }
    else if (currentStatus == WIFI_DISCONNECTED)
    {
        // Langsom blink når disconnected
        unsigned long currentMillis = millis();
        if (currentMillis - previousMillis >= LED_BLINK_SLOW)
        {
            previousMillis = currentMillis;
            ledState = !ledState;
            digitalWrite(LED_PIN, ledState);
        }
    }

    // Handle button press
    handleButtonPress();

    // Monitor connection
    checkWiFiStatus();
}

void BroadBuddyWiFiManager::createBlinkTask()
{
    // Sørg for at der ikke allerede er en task
    if (blinkTaskHandle != NULL)
    {
        vTaskDelete(blinkTaskHandle);
        blinkTaskHandle = NULL;
    }

    xTaskCreate([](void *parameter)
                {
    BroadBuddyWiFiManager* self = (BroadBuddyWiFiManager*) parameter;
    unsigned long previousMillis = 0;
    bool ledState = LOW;
    
    while (self->getStatus() == WIFI_CONNECTING) {
      unsigned long currentMillis = millis();
      if (currentMillis - previousMillis >= LED_BLINK_NORMAL) {
        previousMillis = currentMillis;
        ledState = !ledState;
        digitalWrite(LED_PIN, ledState);
      }
      vTaskDelay(10 / portTICK_PERIOD_MS);
    }
    blinkTaskHandle = NULL;
    vTaskDelete(NULL); }, "blinkTask", 1024, this, 1, &blinkTaskHandle);
}

void BroadBuddyWiFiManager::handleButtonPress()
{
    if (digitalRead(BUTTON_PIN) == LOW)
    {
        if (!buttonPressed)
        {
            buttonPressStart = millis();
            buttonPressed = true;
        }
        else if (millis() - buttonPressStart > 5000)
        {
            Serial.println("Factory reset!");

            // Fast blink reset
            for (int i = 0; i < 40; i++)
            {
                digitalWrite(LED_PIN, HIGH);
                delay(50);
                digitalWrite(LED_PIN, LOW);
                delay(50);
            }

            resetRequest = true;
        }
    }
    else
    {
        if (buttonPressed && millis() - buttonPressStart < 1000)
        {
            Serial.println("Starting config portal");
            currentStatus = WIFI_CONNECTING;
            connectStartTime = millis();

            // Start ny blink task før vi starter config portal
            createBlinkTask();

            // Kør portal i et loop - hvis exit trykkes, scanner og genstarter den
            do
            {
                // Scan efter netværk, så listen er frisk hver gang
                Serial.println("Scanning for networks...");
                int n = WiFi.scanNetworks();
                Serial.print("Found ");
                Serial.print(n);
                Serial.println(" networks");

                // Hvis portal afsluttes uden forbindelse, kommer vi herind igen
                if (!wifiManager.startConfigPortal(AP_NAME, AP_PASSWORD))
                {
                    Serial.println("Portal exited - rescanning networks and restarting");
                    delay(500);
                    // Loop fortsætter
                }
                else
                {
                    // Hvis vi kommer hertil, er der forbundet
                    Serial.println("Connected!");
                    currentStatus = WIFI_CONNECTED;
                    break; // Afslut loop ved forbindelse
                }
            } while (true);
        }
        buttonPressed = false;
    }
}

void BroadBuddyWiFiManager::checkWiFiStatus()
{
    if (millis() - lastWiFiCheck > WIFI_CHECK_INTERVAL)
    {
        lastWiFiCheck = millis();

        if (WiFi.status() == WL_CONNECTED)
        {
            Serial.printf("WiFi OK - Signal: %d dBm\n", WiFi.RSSI());
            if (currentStatus != WIFI_CONNECTED)
            {
                currentStatus = WIFI_CONNECTED;
            }
        }
        else
        {
            Serial.println("WiFi disconnected!");
            if (currentStatus == WIFI_CONNECTED)
            {
                currentStatus = WIFI_DISCONNECTED;
            }
        }
    }
}

WiFiStatus BroadBuddyWiFiManager::getStatus() const
{
    return currentStatus;
}

bool BroadBuddyWiFiManager::resetRequested() const
{
    return resetRequest;
}

void BroadBuddyWiFiManager::resetSettings()
{
    wifiManager.resetSettings();
    WiFi.disconnect(true, true);
    resetRequest = false;
}