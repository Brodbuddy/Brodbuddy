#ifndef EPAPER_MONITOR_H
#define EPAPER_MONITOR_H

#include <Arduino.h>
#include "display/epaper_display.h"
#include <utils/constants.h>

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
    int growthValues[MonitoringConstants::MAX_DATA_POINTS];
    unsigned long timestamps[MonitoringConstants::MAX_DATA_POINTS]; // Tidsstempler i sekunder (millis()/1000)
    int dataCount;
    int oldestIndex;
    bool bufferFull;
};

class EpaperMonitor
{
public:
    EpaperMonitor(EpaperDisplay &display);
    void addDataPoint(SourdoughData &data, int growthPercentage, unsigned long timestamp);
    void updateDisplay(const SourdoughData &data);
    SourdoughData generateMockData();
    void updatePeakInfo(SourdoughData &data);

private:
    EpaperDisplay &_display;
    void drawHeader(const SourdoughData &data);
    void drawBattery(int level);
    void drawGraph(const SourdoughData &data);
};

#endif