#ifndef SOURDOUGH_MONITOR_H
#define SOURDOUGH_MONITOR_H

#include <Arduino.h>
#include "display/SourdoughDisplay.h"

// Maksimalt antal datapunkter der kan gemmes for 12 timer
// Ved 5 minutters interval = 144 punkter
#define MAX_DATA_POINTS 144

struct SourdoughData
{
    // Temperatur og fugtighed
    float outTemp;
    int outHumidity;
    float inTemp;
    int inHumidity;

    // Batteri
    int batteryLevel;

    // Aktuelle vækstværdier
    int currentGrowth;
    int peakGrowth;
    float peakHoursAgo;

    // Cirkulær buffer til at gemme vækstdata over tid
    int growthValues[MAX_DATA_POINTS];
    unsigned long timestamps[MAX_DATA_POINTS]; // Tidsstempler i sekunder (millis()/1000)
    int dataCount;
    int oldestIndex;
    bool bufferFull;
};

class SourdoughMonitor
{
public:
    SourdoughMonitor(SourdoughDisplay &display);
    void addDataPoint(SourdoughData &data, int growthPercentage, unsigned long timestamp);
    void updateDisplay(const SourdoughData &data);
    SourdoughData generateMockData();
    void updatePeakInfo(SourdoughData &data);

private:
    SourdoughDisplay &_display;
    void drawHeader(const SourdoughData &data);
    void drawBattery(int level);
    void drawGraph(const SourdoughData &data);
};

#endif