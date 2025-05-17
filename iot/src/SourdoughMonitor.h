#ifndef SOURDOUGH_MONITOR_H
#define SOURDOUGH_MONITOR_H

#include <Arduino.h>
#include "SourdoughDisplay.h"

struct SourdoughData {
    // Temperatur og fugtighed
    float outTemp;
    int outHumidity;
    float inTemp;
    int inHumidity;

    // Vækstdata
    int currentGrowth;
    int peakGrowth;
    float peakHoursAgo;

    // Batteri
    int batteryLevel;

    // Vækstdatapunkter
    const int* growthData;
    int growthDataSize;
};


class SourdoughMonitor {
public:
    SourdoughMonitor(SourdoughDisplay& display);
    void updateDisplay(const SourdoughData& data);
    SourdoughData generateMockData();    
    int readBatteryLevel();
private:
    SourdoughDisplay& _display;
    void drawHeader(const SourdoughData& data);    
    void drawBattery(int level);
    void drawGraph(const SourdoughData& data);
};

#endif
