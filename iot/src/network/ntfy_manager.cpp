#include "app/ntfy_manager.h"
#include "config/time_utils.h"
#include "logging/logger.h"

static const char* TAG = "NtfyManager";
static const char* NTFY_BASE_URL = "https://ntfy.sh/";

NtfyManager::NtfyManager(const String& topic) 
    : _topic(topic), _decreaseNotificationSent(false),
      _lastRiseValue(-999), _consecutiveDecreases(0), _lastNotificationTime(0) { // _lastRiseValue sættes til -999 da 0 % rise er en valid reading
}

void NtfyManager::checkRiseValue(float currentRise) {
    LOG_D(TAG, "Checking rise value: current=%.2f, last=%.2f, consecutive decreases=%d", 
          currentRise, _lastRiseValue, _consecutiveDecreases);
    
    if (_lastRiseValue > -999) {  
        if (currentRise < _lastRiseValue) {
            _consecutiveDecreases++;
            
            if (_consecutiveDecreases >= 3 && !_decreaseNotificationSent && !isInCooldown()) {
                LOG_I(TAG, "Triggering notification for 3 consecutive decreases");
                sendNotification("Din surdej er klar til brug");
                _decreaseNotificationSent = true;
            } else if (_consecutiveDecreases >= 3 && isInCooldown()) {
                // Surdej er klar, men vi venter med at sende ny notifikation (1 times pause)
                unsigned long remainingCooldown = (TimeUtils::to_ms(TimeConstants::NOTIFICATION_COOLDOWN) - (millis() - _lastNotificationTime)) / 1000 / 60;
            }
        } else {
            if (currentRise > _lastRiseValue) {
                    (_lastRiseValue, currentRise);
            }
            _consecutiveDecreases = 0;
            _decreaseNotificationSent = false;  
        }
    } else {
        LOG_D(TAG, "First reading, initializing last rise value");
    }
    
    _lastRiseValue = currentRise;
}

void NtfyManager::sendNotification(const String& message) {
    String url = String(NTFY_BASE_URL) + _topic;
    
    LOG_I(TAG, "Attempting to send notification to %s: %s", url.c_str(), message.c_str());
    
    _http.begin(url);
    _http.addHeader("Content-Type", "text/plain");
    
    int httpResponseCode = _http.POST(message);
    
    if (httpResponseCode > 0) {
        LOG_I(TAG, "Notification sent successfully, response code: %d", httpResponseCode);
        _lastNotificationTime = millis();  
    } else {
        LOG_E(TAG, "Error sending notification: %d", httpResponseCode);
    }
    
    _http.end();
}

void NtfyManager::reset() {
    LOG_I(TAG, "Resetting NtfyManager state");
    _decreaseNotificationSent = false;
    _consecutiveDecreases = 0;
    _lastRiseValue = -999;  
    _lastNotificationTime = 0;  
}

bool NtfyManager::isInCooldown() const {
    if (_lastNotificationTime == 0) {
        return false; 
    }
    return (millis() - _lastNotificationTime) < TimeUtils::to_ms(TimeConstants::NOTIFICATION_COOLDOWN);
}