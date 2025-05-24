#ifndef CAPTIVE_PORTAL_HTML_H
#define CAPTIVE_PORTAL_HTML_H

#include <Arduino.h>

const char CAPTIVE_PORTAL_HTML[] PROGMEM = R"rawliteral(
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

#endif