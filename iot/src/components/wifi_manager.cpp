#include "components/wifi_manager.h"
#include <Arduino.h>
#include <ArduinoJson.h>

// Custom HTML page for the captive portal
const char html_page[] PROGMEM = R"rawliteral(
<!DOCTYPE html>
<html lang="da">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>BrodBuddy - WiFi Setup</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }

        .container {
            max-width: 400px;
            margin: 0 auto;
            background: white;
            border-radius: 20px;
            box-shadow: 0 10px 30px rgba(0,0,0,0.2);
            overflow: hidden;
        }

        .header {
            background: #667eea;
            color: white;
            padding: 30px 20px;
            text-align: center;
        }

        .logo {
            font-size: 2.5em;
            font-weight: bold;
            margin-bottom: 5px;
        }

        .subtitle {
            opacity: 0.9;
            font-size: 1.1em;
        }

        .content {
            padding: 20px;
        }

        .section {
            margin-bottom: 25px;
        }

        .section-title {
            font-size: 1.2em;
            font-weight: bold;
            color: #333;
            margin-bottom: 15px;
            display: flex;
            align-items: center;
        }

        .icon {
            margin-right: 10px;
            font-size: 1.3em;
        }

        .wifi-list {
            background: #f8f9fa;
            border-radius: 10px;
            max-height: 300px;
            overflow-y: auto;
        }

        .wifi-item {
            padding: 15px;
            border-bottom: 1px solid #e9ecef;
            cursor: pointer;
            display: flex;
            align-items: center;
            justify-content: space-between;
            transition: background-color 0.2s;
        }

        .wifi-item:last-child {
            border-bottom: none;
        }

        .wifi-item:hover {
            background: #e9ecef;
        }

        .wifi-item.selected {
            background: #667eea;
            color: white;
        }

        .wifi-info {
            display: flex;
            align-items: center;
            flex: 1;
        }

        .wifi-name {
            font-weight: 600;
            margin-left: 12px;
        }

        .wifi-signal {
            font-size: 0.9em;
            opacity: 0.7;
        }

        .signal-strength {
            display: flex;
            align-items: center;
            gap: 2px;
        }

        .signal-bar {
            width: 3px;
            background: currentColor;
            border-radius: 1px;
        }

        .signal-bar:nth-child(1) { height: 6px; }
        .signal-bar:nth-child(2) { height: 9px; }
        .signal-bar:nth-child(3) { height: 12px; }
        .signal-bar:nth-child(4) { height: 15px; }

        .login-form {
            background: #f8f9fa;
            padding: 20px;
            border-radius: 10px;
            display: none;
        }

        .login-form.show {
            display: block;
        }

        .form-group {
            margin-bottom: 15px;
        }

        .form-label {
            display: block;
            margin-bottom: 8px;
            font-weight: 500;
            color: #555;
        }

        .form-input {
            width: 100%;
            padding: 12px 15px;
            border: 2px solid #e9ecef;
            border-radius: 8px;
            font-size: 16px;
            transition: border-color 0.2s;
        }

        .form-input:focus {
            outline: none;
            border-color: #667eea;
        }

        .btn {
            background: #667eea;
            color: white;
            border: none;
            padding: 12px 25px;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            width: 100%;
            transition: background-color 0.2s;
        }

        .btn:hover {
            background: #5a6fd8;
        }

        .btn:disabled {
            background: #ccc;
            cursor: not-allowed;
        }

        .btn-secondary {
            background: #6c757d;
            margin-top: 10px;
        }

        .btn-secondary:hover {
            background: #5a6268;
        }

        .loading {
            display: none;
            text-align: center;
            padding: 20px;
        }

        .spinner {
            width: 40px;
            height: 40px;
            border: 4px solid #f3f3f3;
            border-top: 4px solid #667eea;
            border-radius: 50%;
            animation: spin 1s linear infinite;
            margin: 0 auto 15px;
        }

        @keyframes spin {
            0% { transform: rotate(0deg); }
            100% { transform: rotate(360deg); }
        }

        .status-message {
            padding: 15px;
            border-radius: 8px;
            margin-top: 15px;
            display: none;
        }

        .status-success {
            background: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }

        .status-error {
            background: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }

        .refresh-btn {
            background: none;
            border: none;
            color: #667eea;
            cursor: pointer;
            font-size: 1em;
            padding: 5px;
            border-radius: 5px;
            transition: background-color 0.2s;
        }

        .refresh-btn:hover {
            background: #f8f9fa;
        }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <div class="logo">📊 BrodBuddy</div>
            <div class="subtitle">WiFi Opsætning</div>
        </div>
        
        <div class="content">
            <!-- WiFi Networks Section -->
            <div class="section">
                <div class="section-title">
                    <span class="icon">📶</span>
                    Tilgængelige netværk
                    <button class="refresh-btn" onclick="scanNetworks()" title="Opdater liste">
                        🔄
                    </button>
                </div>
                <div class="wifi-list" id="wifiList">
                    <div class="loading" id="scanLoading">
                        <div class="spinner"></div>
                        <div>Søger efter netværk...</div>
                    </div>
                </div>
            </div>

            <!-- Skjult netværk sektion -->
<div class="section">
    <div class="section-title">
        <span class="icon">🔍</span>
        Skjult netværk
    </div>
    <button class="btn" onclick="showHiddenNetworkForm()" id="hiddenNetworkBtn">
        Forbind til skjult netværk
    </button>
    <div class="login-form" id="hiddenNetworkForm" style="display: none;">
        <form onsubmit="connectToHiddenWifi(event)">
            <div class="form-group">
                <label class="form-label" for="hidden-ssid">Netværksnavn (SSID)</label>
                <input type="text" id="hidden-ssid" class="form-input" required>
            </div>
            <div class="form-group">
                <label class="form-label" for="hidden-password">Adgangskode</label>
                <input type="password" id="hidden-password" class="form-input" required>
            </div>
            <button type="submit" class="btn" id="hiddenConnectBtn">
                Tilslut til netværk
            </button>
            <button type="button" class="btn btn-secondary" onclick="hideHiddenNetworkForm()">
                Annuller
            </button>
        </form>
    </div>
</div>

            <!-- Login Form -->
            <div class="section">
                <div class="login-form" id="loginForm">
                    <div class="section-title">
                        <span class="icon">🔑</span>
                        Log ind på <span id="selectedNetwork"></span>
                    </div>
                    <form onsubmit="connectToWifi(event)">
                        <div class="form-group">
                            <label class="form-label" for="ssid">Netværksnavn</label>
                            <input type="text" id="ssid" class="form-input" readonly>
                        </div>
                        <div class="form-group">
                            <label class="form-label" for="password">Adgangskode</label>
                            <input type="password" id="password" class="form-input" required>
                        </div>
                        <button type="submit" class="btn" id="connectBtn">
                            Tilslut til netværk
                        </button>
                        <button type="button" class="btn btn-secondary" onclick="clearSelection()">
                            Annuller
                        </button>
                    </form>
                </div>
            </div>

            <!-- Connection Status -->
            <div class="loading" id="connectLoading">
                <div class="spinner"></div>
                <div>Opretter forbindelse...</div>
            </div>

            <div class="status-message" id="statusMessage"></div>
        </div>
    </div>

    <script>
        let selectedSSID = '';
        let networks = [];

        function scanNetworks() {
            const loadingDiv = document.getElementById('scanLoading');
            const wifiList = document.getElementById('wifiList');
            
            loadingDiv.style.display = 'block';
            
            fetch('/scan')
                .then(response => response.json())
                .then(data => {
                    networks = data.networks || [];
                    displayNetworks();
                })
                .catch(error => {
                    console.error('Fejl ved scanning:', error);
                    loadingDiv.style.display = 'none';
                    showStatus('Fejl ved scanning af netværk', 'error');
                });
        }

        function displayNetworks() {
            const loadingDiv = document.getElementById('scanLoading');
            const wifiList = document.getElementById('wifiList');
            
            loadingDiv.style.display = 'none';
            
            if (networks.length === 0) {
                wifiList.innerHTML = '<div style="padding: 20px; text-align: center; color: #666;">Ingen netværk fundet</div>';
                return;
            }

            const html = networks.map(network => {
                const signalStrength = getSignalStrength(network.rssi);
                const lockIcon = network.encryption ? '🔒' : '🔓';
                
                return `
                    <div class="wifi-item" onclick="selectNetwork('${network.ssid}')">
                        <div class="wifi-info">
                            <span>${lockIcon}</span>
                            <span class="wifi-name">${network.ssid}</span>
                        </div>
                        <div class="signal-strength">
                            ${generateSignalBars(signalStrength)}
                        </div>
                    </div>
                `;
            }).join('');
            
            wifiList.innerHTML = html;
        }

        function getSignalStrength(rssi) {
            if (rssi >= -50) return 4;
            if (rssi >= -60) return 3;
            if (rssi >= -70) return 2;
            return 1;
        }

        function generateSignalBars(strength) {
            let bars = '';
            for (let i = 1; i <= 4; i++) {
                const opacity = i <= strength ? '1' : '0.3';
                bars += `<div class="signal-bar" style="opacity: ${opacity}"></div>`;
            }
            return bars;
        }

        function showHiddenNetworkForm() {
            document.getElementById('hiddenNetworkForm').style.display = 'block';
            document.getElementById('hiddenNetworkBtn').style.display = 'none';
        }

        function hideHiddenNetworkForm() {
            document.getElementById('hiddenNetworkForm').style.display = 'none';
            document.getElementById('hiddenNetworkBtn').style.display = 'block';
        }

        function selectNetwork(ssid) {
            selectedSSID = ssid;
            document.getElementById('ssid').value = ssid;
            document.getElementById('selectedNetwork').textContent = ssid;
            document.getElementById('loginForm').classList.add('show');
            
            // Update visual selection
            document.querySelectorAll('.wifi-item').forEach(item => {
                item.classList.remove('selected');
            });
            event.currentTarget.classList.add('selected');
            
            // Clear any previous status messages
            hideStatus();
        }

        function clearSelection() {
            selectedSSID = '';
            document.getElementById('loginForm').classList.remove('show');
            document.getElementById('password').value = '';
            document.querySelectorAll('.wifi-item').forEach(item => {
                item.classList.remove('selected');
            });
            hideStatus();
        }

        function connectToWifi(event) {
    event.preventDefault();
    
    const ssid = document.getElementById('ssid').value;
    const password = document.getElementById('password').value;
    const connectBtn = document.getElementById('connectBtn');
    const connectLoading = document.getElementById('connectLoading');
    
    if (!ssid) {
        showStatus('Vælg venligst et netværk først', 'error');
        return;
    }
    
    // Show loading state
    connectBtn.disabled = true;
    connectLoading.style.display = 'block';
    hideStatus();
    
    // Send credentials to ESP32
    fetch('/connect', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            ssid: ssid,
            password: password
        })
    })
    .then(response => response.json())
    .then(data => {
        connectLoading.style.display = 'none';
        connectBtn.disabled = false;
        
        if (data.success) {
            let message = `Forbundet til ${ssid}!`;
            if (data.ip) {
                message += ` IP: ${data.ip}`;
            }
            
            // Tilføj vejledning om at forlade BrodBuddy WiFi
            message += ` Din enhed skal nu frakobles fra BrodBuddy_setup WiFi for at forbinde til dit hjemmenetværk.`;
            
            showStatus(message, 'success');
            
            // Start poller for at tjekke forbindelsesstatus
            pollConnectionStatus(ssid, data.ip);
        } else {
            showStatus(`Fejl: ${data.message}`, 'error');
        }
    })
    .catch(error => {
        connectLoading.style.display = 'none';
        connectBtn.disabled = false;
        showStatus('Forbindelsesfejl. Prøv igen.', 'error');
        console.error('Connection error:', error);
    });
}

     function connectToHiddenWifi(event) {
    event.preventDefault();
    
    const ssid = document.getElementById('hidden-ssid').value;
    const password = document.getElementById('hidden-password').value;
    const connectBtn = document.getElementById('hiddenConnectBtn');
    const connectLoading = document.getElementById('connectLoading');
    
    if (!ssid) {
        showStatus('Indtast venligst et netværksnavn', 'error');
        return;
    }
    
    // Show loading state
    connectBtn.disabled = true;
    connectLoading.style.display = 'block';
    hideStatus();
    
    // Send credentials to ESP32
    fetch('/connect', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            ssid: ssid,
            password: password,
            hidden: true
        })
    })
    .then(response => response.json())
    .then(data => {
        connectLoading.style.display = 'none';
        connectBtn.disabled = false;
        
        if (data.success) {
            let message = `Forbundet til ${ssid}!`;
            if (data.ip) {
                message += ` IP: ${data.ip}`;
            }
            
            // Tilføj vejledning om at forlade BrodBuddy WiFi
            message += ` Din enhed skal nu frakobles fra BrodBuddy_setup WiFi for at forbinde til dit hjemmenetværk.`;
            
            showStatus(message, 'success');
            
            // Start poller for at tjekke forbindelsesstatus
            pollConnectionStatus(ssid, data.ip);
        } else {
            showStatus(`Fejl: ${data.message}`, 'error');
        }
    })
    .catch(error => {
        connectLoading.style.display = 'none';
        connectBtn.disabled = false;
        showStatus('Forbindelsesfejl. Prøv igen.', 'error');
        console.error('Connection error:', error);
    });
}

function pollConnectionStatus(ssid, ipAddress) {
    // Forklar brugeren at de skal skifte netværk
    showStatus(`Forbundet til ${ssid}! Gå til dine WiFi indstillinger og skift tilbage til dit normale netværk. Derefter kan du besøge http://${ipAddress} i din browser.`, 'success');
    
    // Vis yderligere instruktioner
    const statusDiv = document.getElementById('statusMessage');
    const instructionsDiv = document.createElement('div');
    instructionsDiv.style.marginTop = '15px';
    instructionsDiv.innerHTML = `
        <ol style="margin-left: 20px;">
            <li>Åbn dine WiFi-indstillinger</li>
            <li>Frakobl fra "BrodBuddy_setup" netværket</li>
            <li>Forbind til dit hjemmenetværk igen</li>
            <li>Åbn adressen <strong>http://${ipAddress}</strong> i din browser</li>
        </ol>
    `;
    statusDiv.appendChild(instructionsDiv);
}


        function showStatus(message, type) {
            const statusDiv = document.getElementById('statusMessage');
            statusDiv.textContent = message;
            statusDiv.className = `status-message status-${type}`;
            statusDiv.style.display = 'block';
        }

        function hideStatus() {
            document.getElementById('statusMessage').style.display = 'none';
        }

        // Auto-scan on page load
        window.addEventListener('load', () => {
            scanNetworks();
        });
    </script>
</body>
</html>
)rawliteral";

TaskHandle_t blinkTaskHandle = NULL;

BroadBuddyWiFiManager::BroadBuddyWiFiManager() : 
  currentStatus(WIFI_DISCONNECTED),
  connectStartTime(0),
  previousMillis(0),
  ledState(LOW),
  lastWiFiCheck(0),
  buttonPressed(false),
  buttonPressStart(0),
  resetRequest(false),
  server(nullptr),
  dns(nullptr),
  portalRunning(false),
  apModeStartTime(0),
  apModeTimeoutEnabled(true) {
}

void BroadBuddyWiFiManager::setup() {
  pinMode(LED_PIN, OUTPUT);
  pinMode(BUTTON_PIN, INPUT_PULLUP);
  
  // Check for reset button - if held down during startup
  if (digitalRead(BUTTON_PIN) == LOW) {
    Serial.println("Reset button pressed during startup - clearing WiFi settings");
    resetSettings();
    delay(1000);
    ESP.restart();
  }
  
  currentStatus = WIFI_CONNECTING;
  ledState = LOW;
  digitalWrite(LED_PIN, ledState);
  connectStartTime = millis();
  
  // Create blink task
  createBlinkTask();
  
  // Try to connect with saved credentials first
  Serial.println("Attempting to connect with saved credentials");
  
  // Try to connect with saved credentials from Preferences
  preferences.begin("wifi", false);
  String ssid = preferences.getString("ssid", "");
  String password = preferences.getString("password", "");
  preferences.end();
  
  if (ssid.length() > 0) {
    Serial.print("Found saved SSID: ");
    Serial.println(ssid);
    WiFi.begin(ssid.c_str(), password.c_str());
  } else {
    WiFi.begin();
  }
  
  // Wait for connection with timeout
  int timeout = 10; // 10 seconds timeout
  while (WiFi.status() != WL_CONNECTED && timeout > 0) {
    delay(1000);
    Serial.print(".");
    timeout--;
  }
  
  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\nConnected to WiFi!");
    Serial.print("IP address: ");
    Serial.println(WiFi.localIP());
    
    // Set hostname for mDNS
    WiFi.setHostname(HOSTNAME);
    
    currentStatus = WIFI_CONNECTED;
    digitalWrite(LED_PIN, HIGH);
  } else {
    // If connection fails, start the custom portal
    Serial.println("Failed to connect with saved credentials, starting custom portal");
    startCustomPortal();
  }
}

void BroadBuddyWiFiManager::loop() {
  // Handle DNS and webserver if portal is running
 if (portalRunning && server != nullptr && dns != nullptr) {
    dns->processNextRequest();
    server->handleClient();
    
    // Check for AP mode timeout
    if (apModeTimeoutEnabled && (millis() - apModeStartTime > AP_MODE_TIMEOUT)) {
      Serial.println("AP mode timeout - restarting device");
      delay(500);
      ESP.restart();  // Genstarter enheden - alternativt kunne du gå tilbage til normal drift
    }
  }

  // LED handling
  if (currentStatus == WIFI_CONNECTED) {
    digitalWrite(LED_PIN, HIGH);  
  } else if (currentStatus == WIFI_CONNECTING && (millis() - connectStartTime < 30000)) {
    // Backup blinking (if task fails)
    unsigned long currentMillis = millis();
    if (currentMillis - previousMillis >= LED_BLINK_NORMAL) {
      previousMillis = currentMillis;
      ledState = !ledState;
      digitalWrite(LED_PIN, ledState);
    }
  } else if (currentStatus == WIFI_DISCONNECTED) {
    // Slow blink when disconnected
    unsigned long currentMillis = millis();
    if (currentMillis - previousMillis >= LED_BLINK_SLOW) {
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

void BroadBuddyWiFiManager::startCustomPortal() {
  // Disconnect from any existing networks
  WiFi.disconnect();
  
  // Set WiFi to AP mode
  WiFi.mode(WIFI_AP);
  WiFi.softAP(AP_NAME, AP_PASSWORD);
  
  Serial.print("AP IP address: ");
  Serial.println(WiFi.softAPIP());
  
  // Initialize DNS server and web server
  if (dns == nullptr) dns = new DNSServer();
  if (server == nullptr) server = new WebServer(80);
  
  // Configure DNS to redirect all requests to captive portal
  dns->setErrorReplyCode(DNSReplyCode::NoError);
  dns->start(53, "*", WiFi.softAPIP());
  
  // Set up web server routes
  setupWebServer();
  
  // Start web server
  server->begin();
  portalRunning = true;
  
  // Update status
  currentStatus = WIFI_CONNECTING;
  connectStartTime = millis();
  apModeStartTime = millis();
  
  // Create blink task
  createBlinkTask();
  
  Serial.println("Custom portal started");
  Serial.println("Connect to WiFi: " + String(AP_NAME));
  Serial.println("Then go to: http://192.168.4.1");
}

void BroadBuddyWiFiManager::stopCustomPortal() {
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

void BroadBuddyWiFiManager::setupWebServer() {
  // Serve the root HTML page
  server->on("/", HTTP_GET, [this]() {
    this->handleRoot();
  });
  
  // Handle WiFi scan requests
  server->on("/scan", HTTP_GET, [this]() {
    this->handleScan();
  });
  
  // Handle connection attempts
  server->on("/connect", HTTP_POST, [this]() {
    this->handleConnect();
  });
  
  // Handle connection status requests
  server->on("/connection-status", HTTP_GET, [this]() {
    this->handleConnectionStatus();
  });
  
  // Handle all other URLs (for captive portal functionality)
  server->onNotFound([this]() {
    this->handleNotFound();
  });
}

void BroadBuddyWiFiManager::handleRoot() {
  Serial.println("Serving HTML page");
  server->send(200, "text/html", html_page);
}

void BroadBuddyWiFiManager::handleScan() {
  Serial.println("Scanning networks...");
  int n = WiFi.scanNetworks();
  Serial.printf("Found %d networks\n", n);
  
  // Create JSON response
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
  
  // Clean up scan results
  WiFi.scanDelete();
}

void BroadBuddyWiFiManager::handleConnect() {
  // Parse JSON request
  String data = server->arg("plain");
  DynamicJsonDocument doc(512);
  DeserializationError error = deserializeJson(doc, data);
  
  if (error) {
    Serial.print("deserializeJson() failed: ");
    Serial.println(error.c_str());
    server->send(400, "application/json", "{\"success\":false,\"message\":\"Invalid request format\"}");
    return;
  }
  
  // Extract credentials
  String ssid = doc["ssid"];
  String password = doc["password"];
  bool isHidden = doc.containsKey("hidden") ? doc["hidden"].as<bool>() : false;
  
  if (ssid.length() == 0) {
    server->send(400, "application/json", "{\"success\":false,\"message\":\"SSID is required\"}");
    return;
  }
  
  Serial.print("Attempting to connect to: ");
  Serial.println(ssid);
  
  // Save the credentials to Preferences
  saveWiFiCredentials(ssid, password);
  
  // Send initial response to the client
  server->send(200, "application/json", "{\"success\":true,\"message\":\"Attempting to connect...\",\"ip\":\"192.168.4.1\"}");
  
  // Connect to the WiFi - med support for skjulte netværk
  if (isHidden) {
    WiFi.begin(ssid.c_str(), password.c_str(), 0, NULL, true); // true for hidden networks
  } else {
    WiFi.begin(ssid.c_str(), password.c_str());
  }
  
  // Wait for connection with timeout
  int timeout = 30; // 30 seconds timeout
  while (WiFi.status() != WL_CONNECTED && timeout > 0) {
    delay(1000);
    Serial.print(".");
    timeout--;
  }
  
  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\nConnected to WiFi!");
    Serial.print("IP address: ");
    Serial.println(WiFi.localIP());
    
    // Set hostname
    WiFi.setHostname(HOSTNAME);
    
    // Enable auto-reconnect
    WiFi.setAutoReconnect(true);
    
    // Opret et JSON-svar med succesmeddelelse og vejledning
    DynamicJsonDocument responseDoc(256);
    responseDoc["success"] = true;
    responseDoc["ip"] = WiFi.localIP().toString();
    responseDoc["message"] = "Forbundet til " + ssid + "! Du kan nu forlade opsætningssiden.";
    responseDoc["disconnectAP"] = true;  // Flag til at telefonen skal frakoble
    
    String jsonResponse;
    serializeJson(responseDoc, jsonResponse);
    
    // Send dette JSON svar til klienten før portalen lukkes
    if (server != nullptr) {
      // Vi sender det ikke direkte her, da den indledende respons allerede er sendt
      // Men vi gemmer det til /connection-status endpoint
      lastConnectionStatus = jsonResponse;
    }
    
    // Update status
    currentStatus = WIFI_CONNECTED;
    digitalWrite(LED_PIN, HIGH);
    
    // Vent et øjeblik før portal lukkes
    delay(1000);
    
    // Stop portal
    stopCustomPortal();
  } else {
    Serial.println("\nFailed to connect");
    
    // Log fejlen mere detaljeret
    switch (WiFi.status()) {
      case WL_NO_SSID_AVAIL:
        Serial.println("Error: Network not found");
        break;
      case WL_CONNECT_FAILED:
        Serial.println("Error: Connection failed, likely incorrect password");
        break;
      case WL_CONNECTION_LOST:
        Serial.println("Error: Connection lost");
        break;
      case WL_DISCONNECTED:
        Serial.println("Error: Disconnected");
        break;
      default:
        Serial.printf("Error: Status code %d\n", WiFi.status());
    }
    
    // Genstart AP-mode
    WiFi.disconnect();
    WiFi.mode(WIFI_AP);
    WiFi.softAP(AP_NAME, AP_PASSWORD);
  }
}

void BroadBuddyWiFiManager::handleNotFound() {
  // For captive portal functionality, redirect all requests to the portal page
  Serial.print("Handling not found for URL: ");
  Serial.println(server->uri());
  server->sendHeader("Location", "http://192.168.4.1/", true);
  server->send(302, "text/plain", "");
}

void BroadBuddyWiFiManager::handleConnectionStatus() {
  DynamicJsonDocument statusDoc(256);
  
  if (WiFi.status() == WL_CONNECTED) {
    statusDoc["connected"] = true;
    statusDoc["ip"] = WiFi.localIP().toString();
    statusDoc["disconnectAP"] = true;  // Signal til klienten om at frakoble fra AP
    statusDoc["message"] = "Forbundet til WiFi! Du kan nu lukke denne side.";
  } else {
    statusDoc["connected"] = false;
    
    // Giv en detaljeret fejlmeddelelse
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

void BroadBuddyWiFiManager::createBlinkTask() {
  // Ensure there isn't already a task
  if (blinkTaskHandle != NULL) {
    vTaskDelete(blinkTaskHandle);
    blinkTaskHandle = NULL;
  }
  
  xTaskCreate([](void * parameter) {
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
    vTaskDelete(NULL);
  }, "blinkTask", 1024, this, 1, &blinkTaskHandle);
}

void BroadBuddyWiFiManager::handleButtonPress() {
  if (digitalRead(BUTTON_PIN) == LOW) {
    if (!buttonPressed) {
      buttonPressStart = millis();
      buttonPressed = true;
    } else if (millis() - buttonPressStart > 5000) {
      Serial.println("Factory reset!");
      
      // Fast blink reset
      for (int i = 0; i < 40; i++) {
        digitalWrite(LED_PIN, HIGH);
        delay(50);
        digitalWrite(LED_PIN, LOW);
        delay(50);
      }
      
      resetRequest = true;
    }
  } else {
    if (buttonPressed && millis() - buttonPressStart < 1000) {
      Serial.println("Starting config portal");
      
      // Start the custom portal
      startCustomPortal();
    }
    buttonPressed = false;
  }
}

void BroadBuddyWiFiManager::checkWiFiStatus() {
  if (millis() - lastWiFiCheck > WIFI_CHECK_INTERVAL) {
    lastWiFiCheck = millis();
    
    if (WiFi.status() == WL_CONNECTED) {
      Serial.printf("WiFi OK - Signal: %d dBm\n", WiFi.RSSI());
      if (currentStatus != WIFI_CONNECTED) {
        currentStatus = WIFI_CONNECTED;
      }
    } else {
      Serial.println("WiFi disconnected!");
      if (currentStatus == WIFI_CONNECTED) {
        currentStatus = WIFI_DISCONNECTED;
      }
    }
  }
}

WiFiStatus BroadBuddyWiFiManager::getStatus() const {
  return currentStatus;
}

bool BroadBuddyWiFiManager::resetRequested() const {
  return resetRequest;
}

void BroadBuddyWiFiManager::saveWiFiCredentials(const String& ssid, const String& password) {
  preferences.begin("wifi", false);
  preferences.putString("ssid", ssid);
  preferences.putString("password", password);
  preferences.end();
}

void BroadBuddyWiFiManager::resetSettings() {
  WiFi.disconnect(true, true);  
  preferences.begin("wifi", false);
  preferences.clear();
  preferences.end();
  
  resetRequest = false;
}

void BroadBuddyWiFiManager::enableAPModeTimeout() {
  apModeTimeoutEnabled = true;
  apModeStartTime = millis();
}

void BroadBuddyWiFiManager::disableAPModeTimeout() {
  apModeTimeoutEnabled = false;
}