#include "app/epaper_monitor.h"

#include "config/constants.h"
#include "config/time_utils.h"
#include "logging/logger.h"

static const char* TAG = "EpaperMonitor";

EpaperMonitor::EpaperMonitor(EpaperDisplay& display) : _display(display) {}

SourdoughData EpaperMonitor::generateMockData() {
    const int mockGrowthValues[] = {100, 102, 101, 104, 108, 115, 117, 120, 123, 127, 134, 140, 138, 135, 142,
                                    152, 158, 165, 169, 172, 180, 178, 187, 195, 203, 210, 214, 212, 225, 235,
                                    232, 228, 235, 242, 256, 265, 268, 273, 269, 278, 288, 295, 300, 307, 304,
                                    312, 318, 325, 330, 324, 336, 345, 351, 348, 352, 351, 346, 338, 329, 320,
                                    315, 308, 303, 295, 287, 280, 277, 268, 260, 255, 250, 245};
    const int mockDataSize = sizeof(mockGrowthValues) / sizeof(mockGrowthValues[0]);

    SourdoughData data = {};
    data.dataCount = 0;
    data.oldestIndex = 0;
    data.bufferFull = false;

    data.outTemp = 21.0;
    data.outHumidity = 44;
    data.inTemp = 20.7;
    data.inHumidity = 100;
    data.batteryLevel = 20;

    unsigned long now = millis() / 1000;
    unsigned long windowSeconds = TimeUtils::to_seconds(TimeConstants::GRAPH_WINDOW);
    unsigned long startTime = now - windowSeconds;

    // Tilføj mockdata med 10-minutters intervaller
    unsigned long interval = 10 * 60;
    for (int i = 0; i < mockDataSize; i++) {
        unsigned long timestamp = startTime + (i * interval);
        addDataPoint(data, mockGrowthValues[i], timestamp);
    }

    LOG_D(TAG, "Generated mock data with %d data points", mockDataSize);
    return data;
}

void EpaperMonitor::addDataPoint(SourdoughData& data, int growthPercentage, unsigned long timestamp) {
    int insertIndex;

    if (data.bufferFull) {
        insertIndex = data.oldestIndex;
        data.oldestIndex = (data.oldestIndex + 1) % MonitoringConstants::MAX_DATA_POINTS;
        LOG_D(TAG, "Buffer full, wrapping around. Oldest index now: %d", data.oldestIndex);
    } else {
        insertIndex = data.dataCount;
        data.dataCount++;

        if (data.dataCount >= MonitoringConstants::MAX_DATA_POINTS) {
            data.bufferFull = true;
            data.dataCount = MonitoringConstants::MAX_DATA_POINTS;
            LOG_I(TAG, "Data buffer reached capacity (%d points)", MonitoringConstants::MAX_DATA_POINTS);
        }
    }

    data.growthValues[insertIndex] = growthPercentage;
    data.timestamps[insertIndex] = timestamp;

    data.currentGrowth = growthPercentage;

    updatePeakInfo(data);
}

void EpaperMonitor::updatePeakInfo(SourdoughData& data) {
    int previousPeak = data.peakGrowth;
    data.peakGrowth = 0;
    int peakIndex = -1;

    // Find peak værdi og indeks
    for (int i = 0; i < data.dataCount; i++) {
        int actualIndex = (data.oldestIndex + i) % MonitoringConstants::MAX_DATA_POINTS;
        if (data.growthValues[actualIndex] > data.peakGrowth) {
            data.peakGrowth = data.growthValues[actualIndex];
            peakIndex = actualIndex;
        }
    }

    // Beregn timer siden peak
    if (peakIndex >= 0 && data.dataCount > 0) {
        unsigned long now = 0;
        for (int i = 0; i < data.dataCount; i++) {
            int idx = (data.oldestIndex + i) % MonitoringConstants::MAX_DATA_POINTS;
            if (data.timestamps[idx] > now) {
                now = data.timestamps[idx];
            }
        }
        
        unsigned long peakTime = data.timestamps[peakIndex];
        if (now >= peakTime) {
            data.peakHoursAgo = (now - peakTime) / 3600.0;
        } else {
            data.peakHoursAgo = 0;
        }

        if (data.peakGrowth != previousPeak) {
            LOG_D(TAG, "New peak detected: %d%% (was %d%%)", data.peakGrowth, previousPeak);
        }
    } else {
        data.peakHoursAgo = 0;
    }
}

void EpaperMonitor::updateDisplay(const SourdoughData& data) {
    LOG_I(TAG, "Updating display - Growth: %d%%, Peak: %d%% (%.1fh ago)", data.currentGrowth, data.peakGrowth,
          data.peakHoursAgo);

    _display.clearBuffers();

    // Tegn linje til at adskille header fra grafområdet
    _display.drawLine(2, 35, _display.width() - 3, 35, DisplayConstants::COLOR_BLACK);

    drawHeader(data);
    drawBattery(data.batteryLevel);
    drawGraph(data);

    _display.updateDisplay();
}

void EpaperMonitor::drawHeader(const SourdoughData& data) {
    // Første række: Udendørs temperatur, fugtighed og vækst
    _display.setTextColor(DisplayConstants::COLOR_BLACK);
    _display.setCursor(10, 5);
    _display.print("Out: ");
    _display.print(data.outTemp, 1);
    _display.print("C ");
    _display.print(data.outHumidity);
    _display.print("%");

    _display.setTextColor(DisplayConstants::COLOR_RED);
    _display.setCursor(170, 5);
    _display.print("Growth: ");
    _display.print(data.currentGrowth);
    _display.print("%");

    // Anden række: Indendørs temperatur, fugtighed og peak vækst
    _display.setTextColor(DisplayConstants::COLOR_BLACK);
    _display.setCursor(10, 20);
    _display.print("In: ");
    _display.print(data.inTemp, 1);
    _display.print("C ");
    _display.print(data.inHumidity);
    _display.print("%");

    _display.setTextColor(DisplayConstants::COLOR_RED);
    _display.setCursor(170, 20);
    _display.print(data.peakHoursAgo, 1);
    _display.print("h ago: ");
    _display.print(data.peakGrowth);
    _display.print("%");
}

void EpaperMonitor::drawBattery(int batteryLevel) {
    // Tegn lodret batteri form
    _display.drawRect(270, 5, 10, 20, DisplayConstants::COLOR_BLACK); // Batteriets ydre
    _display.drawRect(272, 2, 6, 3, DisplayConstants::COLOR_BLACK);   // Batteriets top
    // Fyld batteriniveau fra bunden op
    int fillHeight = (int)(batteryLevel / 100.0 * 16);
    _display.fillRect(272, 23 - fillHeight, 6, fillHeight, DisplayConstants::COLOR_BLACK);
}

void EpaperMonitor::drawGraph(const SourdoughData& data) {
    // Grafområdets dimensioner
    int graphX = 10;
    int graphY = 45;
    int graphWidth = _display.width() - 20;
    int graphHeight = _display.height() - graphY - 10;

    // Definer etiketbredde for Y-aksen
    int yLabelWidth = 30;
    int actualGraphWidth = graphWidth - yLabelWidth;

    // Tegn X-aksens etiketter (tid) og lodrette gitterlinjer
    // Start X-etiketter fra en position, der ikke overlapper med Y-etiketter
    int xLabelStartX = graphX + yLabelWidth;

    // Tegn L-formet graframme (|_)
    _display.drawLine(xLabelStartX, graphY, xLabelStartX, graphY + graphHeight, DisplayConstants::COLOR_BLACK);
    _display.drawLine(xLabelStartX, graphY + graphHeight, graphX + graphWidth, graphY + graphHeight,
                      DisplayConstants::COLOR_BLACK);

    unsigned long windowMinutes = TimeUtils::to_minutes(TimeConstants::GRAPH_WINDOW);
    
    int gridInterval;
    if (windowMinutes <= 10) {
        gridInterval = 1;  // Every minute
    } else if (windowMinutes <= 60) {
        gridInterval = 5;  // Every 5 minutes
    } else if (windowMinutes <= 360) {
        gridInterval = 30; // Every 30 minutes
    } else {
        gridInterval = 60; // Every hour
    }
    
    int numGridLines = windowMinutes / gridInterval;
    int gridWidth = actualGraphWidth / numGridLines;
    
    for (int i = 0; i <= numGridLines; i++) {
        int x = xLabelStartX + (i * gridWidth);
        
        // Tegn gitterlinje
        if (i == 0) {
            _display.drawLine(x, graphY - 3, x, graphY + graphHeight, DisplayConstants::COLOR_BLACK);
        } else {
            _display.drawLine(x, graphY, x, graphY + graphHeight, DisplayConstants::COLOR_BLACK);
        }
        
        int minutesFromStart = i * gridInterval;
        int minutesAgo = windowMinutes - minutesFromStart;
        
        bool showLabel = false;
        if (i == numGridLines) {
            showLabel = true; // Always show "now"
        } else if (windowMinutes <= 10 && minutesAgo % 2 == 0) {
            showLabel = true; // For 10m window: 10, 8, 6, 4, 2
        } else if (windowMinutes <= 60 && minutesAgo % 10 == 0) {
            showLabel = true; // For 1h window: 60, 50, 40, 30, 20, 10
        } else if (windowMinutes <= 360 && minutesAgo % 60 == 0) {
            showLabel = true; // For 6h window: 6h, 5h, 4h, 3h, 2h, 1h
        } else if (minutesAgo % 120 == 0) {
            showLabel = true; // For 12h window: 12h, 10h, 8h, 6h, 4h, 2h
        }
        
        if (showLabel) {
            _display.setTextColor(DisplayConstants::COLOR_BLACK);
            
            if (i == numGridLines) {
                _display.setCursor(x - 12, graphY + graphHeight + 2);
                _display.print("now");
            } else if (minutesAgo >= 60) {
                _display.setCursor(x - 6, graphY + graphHeight + 2);
                _display.print(minutesAgo / 60);
                _display.print("h");
            } else {
                if (minutesAgo >= 10) {
                    _display.setCursor(x - 10, graphY + graphHeight + 2);
                } else {
                    _display.setCursor(x - 6, graphY + graphHeight + 2);
                }
                _display.print(minutesAgo);
                _display.print("m");
            }
        }
    }

    // Tegn Y-aksens etiketter (vækstprocent)
    int yLabelValues[] = {100, 150, 200, 250, 300, 350, 400};

    int numYLabels = sizeof(yLabelValues) / sizeof(yLabelValues[0]);
    for (int i = 0; i < numYLabels; i++) {
        int y = graphY + graphHeight - (i * graphHeight / (numYLabels - 1));

        // Tegn vandret gitterlinje - find the rightmost grid line position
        int rightmostX = xLabelStartX + (numGridLines * gridWidth);
        _display.drawLine(xLabelStartX, y, rightmostX, y, DisplayConstants::COLOR_BLACK);

        // Tegn etiket
        _display.setCursor(graphX, y - 3);
        _display.print(yLabelValues[i]);
        _display.print("%");
    }

    // Værdier for skalering
    int maxValue = 400;
    int minValue = 100;
    int valueRange = maxValue - minValue;

    if (data.dataCount == 0) {
        LOG_D(TAG, "No data points to display in graph");
        return;
    }

    // Beregn tidsramme (12 timer total) - find newest timestamp as "now"
    unsigned long now = 0;
    if (data.dataCount > 0) {
        // Find den nyeste timestamp
        for (int i = 0; i < data.dataCount; i++) {
            int idx = (data.oldestIndex + i) % MonitoringConstants::MAX_DATA_POINTS;
            if (data.timestamps[idx] > now) {
                now = data.timestamps[idx];
            }
        }
    } else {
        now = millis() / 1000;
    }
    
    unsigned long windowSeconds = TimeUtils::to_seconds(TimeConstants::GRAPH_WINDOW);
    unsigned long windowStartTime = now - windowSeconds;
    
    LOG_D(TAG, "Graph time window: now=%lu, start=%lu, window=%lus", now, windowStartTime, windowSeconds);
    if (data.dataCount > 0) {
        int firstIdx = data.oldestIndex;
        int lastIdx = (data.oldestIndex + data.dataCount - 1) % MonitoringConstants::MAX_DATA_POINTS;
        LOG_D(TAG, "Data timestamps: first=%lu, last=%lu, count=%d", data.timestamps[firstIdx], data.timestamps[lastIdx], data.dataCount);
    }
    float timeRange = (float)windowSeconds;

    // Kun tegn punkter, hvis vi har mindst 2
    if (data.dataCount >= 2) {
        // Tegn linjerne mellem punkter
        for (int i = 0; i < data.dataCount - 1; i++) {
            // Beregn de faktiske indekser med hensyn til cirkulær buffer
            int idx1 = (data.oldestIndex + i) % MonitoringConstants::MAX_DATA_POINTS;
            int idx2 = (data.oldestIndex + i + 1) % MonitoringConstants::MAX_DATA_POINTS;

            // Beregn relative positioner på tidsaksen
            float timePct1 = (data.timestamps[idx1] - windowStartTime) / timeRange;
            float timePct2 = (data.timestamps[idx2] - windowStartTime) / timeRange;

            // Begræns til gyldig tidsskala (0.0 til 1.0)
            timePct1 = max(0.0f, min(1.0f, timePct1));
            timePct2 = max(0.0f, min(1.0f, timePct2));
            
            if (timePct1 > 1.0f || timePct2 > 1.0f) {
                LOG_D(TAG, "Time percentage out of range: pct1=%.2f, pct2=%.2f", timePct1, timePct2);
            }

            // Beregn x-koordinater baseret på disse tidspositioner
            int rightmostGridX = xLabelStartX + (numGridLines * gridWidth);
            int x1 = xLabelStartX + (int)(timePct1 * (rightmostGridX - xLabelStartX));
            int x2 = xLabelStartX + (int)(timePct2 * (rightmostGridX - xLabelStartX));

            // Beregn y-koordinater baseret på vækstværdier
            int y1 = graphY + graphHeight - ((data.growthValues[idx1] - minValue) * graphHeight / valueRange);
            int y2 = graphY + graphHeight - ((data.growthValues[idx2] - minValue) * graphHeight / valueRange);

            if (i == 0) {
                LOG_D(TAG, "First line: ts1=%lu, ts2=%lu, pct1=%.2f, pct2=%.2f, x1=%d, x2=%d, y1=%d, y2=%d, val1=%d, val2=%d",
                      data.timestamps[idx1], data.timestamps[idx2], timePct1, timePct2, x1, x2, y1, y2,
                      data.growthValues[idx1], data.growthValues[idx2]);
            }

            // Tegn linje mellem punkter
            _display.drawLine(x1, y1, x2, y2, DisplayConstants::COLOR_BLACK);
        }
    }

    // Find peak for at markere den
    int peakValue = 0;
    int peakIndex = -1;

    for (int i = 0; i < data.dataCount; i++) {
        int idx = (data.oldestIndex + i) % MonitoringConstants::MAX_DATA_POINTS;
        if (data.growthValues[idx] > peakValue) {
            peakValue = data.growthValues[idx];
            peakIndex = idx;
        }
    }

    // Hvis vi fandt en peak, marker den
    if (peakIndex >= 0) {
        float peakTimePct = (data.timestamps[peakIndex] - windowStartTime) / timeRange;
        peakTimePct = max(0.0f, min(1.0f, peakTimePct));

        int rightmostGridX = xLabelStartX + (numGridLines * gridWidth);
        int peakX = xLabelStartX + (int)(peakTimePct * (rightmostGridX - xLabelStartX));
        int peakY = graphY + graphHeight - ((peakValue - minValue) * graphHeight / valueRange);

        _display.fillRect(peakX - 3, peakY - 3, 6, 6, DisplayConstants::COLOR_RED);
    }
}