#ifndef EPAPER_MONITOR_H
#define EPAPER_MONITOR_H

#include <Arduino.h>

#include "config/constants.h"
#include "hardware/epaper_display.h"

struct SourdoughData {
    // Temperatur og fugtighed
    float inTemp;
    int inHumidity;

    // Batteri
    int batteryLevel;

    // Aktuelle vækstværdier
    int currentGrowth;
    int peakGrowth;
    float peakHoursAgo;

    int growthValues[MonitoringConstants::MAX_DATA_POINTS];
    unsigned long timestamps[MonitoringConstants::MAX_DATA_POINTS]; 
    int dataCount;
    int oldestIndex;
    bool bufferFull;
};

class EpaperMonitor {
  public:
    EpaperMonitor(EpaperDisplay& display);
    void addDataPoint(SourdoughData& data, int growthPercentage, unsigned long timestamp);
    void updateDisplay(const SourdoughData& data);
    SourdoughData generateMockData();
    void updatePeakInfo(SourdoughData& data);

  private:
    EpaperDisplay& _display;
    void drawHeader(const SourdoughData& data);
    void drawBattery(int level);
    void drawGraph(const SourdoughData& data);
};

#endif