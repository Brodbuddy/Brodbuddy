#ifndef NTFY_MANAGER_H
#define NTFY_MANAGER_H

#include <Arduino.h>
#include <HTTPClient.h>

class NtfyManager {
private:
    String _topic;
    bool _decreaseNotificationSent;
    HTTPClient _http;
    
    float _lastRiseValue;
    int _consecutiveDecreases;
    unsigned long _lastNotificationTime;


public:
    NtfyManager(const String& topic);
    void checkRiseValue(float currentRise);
    void sendNotification(const String& message);
    void reset();
    bool isInCooldown() const;

};

#endif