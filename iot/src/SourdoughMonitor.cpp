#include "SourdoughMonitor.h"

const int mockGrowthData[] = {
  100, 102, 101, 104, 108, 115,
  117, 120, 123, 127, 134, 140,
  138, 135, 142, 152, 158, 165,
  169, 172, 180, 178, 187, 195,
  203, 210, 214, 212, 225, 235,
  232, 228, 235, 242, 256, 265,
  268, 273, 269, 278, 288, 295,
  300, 307, 304, 312, 318, 325,
  330, 324, 336, 345, 351, 348,
  352, 351, 346, 338, 329, 320,
  315, 308, 303, 295, 287, 280,
  277, 268, 260, 255, 250, 245
};

const int NUM_DETAILED_POINTS = sizeof(mockGrowthData) / sizeof(mockGrowthData[0]);

SourdoughMonitor::SourdoughMonitor(SourdoughDisplay& display) : _display(display) {}

SourdoughData SourdoughMonitor::generateMockData() {
    SourdoughData data;
    
    data.outTemp = 21.0;
    data.outHumidity = 44;
    data.inTemp = 20.7;
    data.inHumidity = 100;
    data.batteryLevel = 55;
    
    data.currentGrowth = mockGrowthData[NUM_DETAILED_POINTS - 1];
    
    data.peakGrowth = 0;
    int peakIndex = 0;
    for (int i = 0; i < NUM_DETAILED_POINTS; i++) {
        if (mockGrowthData[i] > data.peakGrowth) {
            data.peakGrowth = mockGrowthData[i];
            peakIndex = i;
        }
    }
    
    data.peakHoursAgo = peakIndex / 6.0;
    
    data.growthData = mockGrowthData;
    data.growthDataSize = NUM_DETAILED_POINTS;
    
    return data;
}

int SourdoughMonitor::readBatteryLevel() {
    return 55;
}

void SourdoughMonitor::updateDisplay(const SourdoughData& data) {
    _display.clearBuffers();
    
    // Tegn linje til at adskille header fra grafområdet
    _display.drawLine(2, 35, _display.width()-3, 35, COLOR_BLACK);
    
    drawHeader(data);
    drawBattery(data.batteryLevel);    
    drawGraph(data);
    
    _display.updateDisplay();
}

void SourdoughMonitor::drawHeader(const SourdoughData& data) {
    // Første række: Udendørs temperatur, fugtighed og vækst
    _display.setTextColor(COLOR_BLACK);
    _display.setCursor(10, 5);
    _display.print("Out: ");
    _display.print(data.outTemp, 1);
    _display.print("C ");
    _display.print(data.outHumidity);
    _display.print("%");
    
    _display.setTextColor(COLOR_RED);
    _display.setCursor(170, 5);
    _display.print("Growth: ");
    _display.print(data.currentGrowth);
    _display.print("%");
    
    // Anden række: Indendørs temperatur, fugtighed og peak vækst
    _display.setTextColor(COLOR_BLACK);
    _display.setCursor(10, 20);
    _display.print("In: ");
    _display.print(data.inTemp, 1);
    _display.print("C ");
    _display.print(data.inHumidity);
    _display.print("%");
    
    _display.setTextColor(COLOR_RED);
    _display.setCursor(170, 20);
    _display.print(data.peakHoursAgo, 1);
    _display.print("h ago: ");
    _display.print(data.peakGrowth);
    _display.print("%");
}


void SourdoughMonitor::drawBattery(int batteryLevel) {
    // Tegn batteriniveau (lodret, 55% fyldt)
    // Tegn lodret batteri form
    _display.drawRect(270, 5, 10, 20, COLOR_BLACK);          // Batteriets ydre
    _display.drawRect(272, 2, 6, 3, COLOR_BLACK);            // Batteriets top
    // Fyld batteriniveau fra bunden op
    int fillHeight = (int)(batteryLevel/100.0 * 16);
    _display.fillRect(272, 23 - fillHeight, 6, fillHeight, COLOR_BLACK);
}

void SourdoughMonitor::drawGraph(const SourdoughData& data) {
    // Grafområdets dimensioner
    int graphX = 10;
    int graphY = 45;
    int graphWidth = _display.width() - 20;
    int graphHeight = _display.height() - graphY - 10;
    
    const int hourlyPoints = data.growthDataSize / 6; // 12 timer + nutid
    int growthData[hourlyPoints];
    
    for (int i = 0; i < hourlyPoints; i++) {
        // Tag én prøve for hver 6 datapunkter (hver time)
        int dataIndex = i * 6;
        if (dataIndex < data.growthDataSize) {
            growthData[i] = data.growthData[dataIndex];
        }
    }
    int numPoints = hourlyPoints;
    
    // Definer etiketbredde for Y-aksen
    int yLabelWidth = 30;
    
    // Tegn X-aksens etiketter (tid) og lodrette gitterlinjer
    char timeLabels[7][5] = {"12h", "10h", "8h", "6h", "4h", "2h", "now"};
    
    // Start X-etiketter fra en position, der ikke overlapper med Y-etiketter
    int xLabelStartX = graphX + yLabelWidth;
    
    // Tegn L-formet graframme (|_)
    _display.drawLine(xLabelStartX, graphY, xLabelStartX, graphY + graphHeight, COLOR_BLACK);
    _display.drawLine(xLabelStartX, graphY + graphHeight, graphX + graphWidth, graphY + graphHeight, COLOR_BLACK);
    
    // Tilføj lodrette gitterlinjer for hver time (12 timer i alt)
    int hourWidth = (graphWidth - yLabelWidth) / 12;
    
    for (int hour = 0; hour <= 12; hour++) {
        int x = xLabelStartX + (hour * hourWidth);
        
        // Tegn gitterlinje for hver time
        _display.drawLine(x, graphY, x, graphY + graphHeight, COLOR_BLACK);
        
        // Tilføj etiketter kun for lige timer
        if (hour % 2 == 0) {
            int labelIndex = hour / 2;
            if (labelIndex < sizeof(timeLabels)/sizeof(timeLabels[0])) {
                _display.setCursor(x - 8, graphY + graphHeight + 2);
                _display.setTextColor(COLOR_BLACK);
                _display.print(timeLabels[labelIndex]);
            }
        }
    }
    
    // Tegn Y-aksens etiketter (vækstprocent)
    int yLabelValues[] = {150, 200, 250, 300, 350, 400};
    
    int numYLabels = sizeof(yLabelValues) / sizeof(yLabelValues[0]);
    for (int i = 0; i < numYLabels; i++) {
        int y = graphY + graphHeight - (i+1) * graphHeight / numYLabels;
        
        // Tegn vandret gitterlinje
        _display.drawLine(xLabelStartX, y, graphX + graphWidth, y, COLOR_BLACK);
        
        // Tegn etiket
        _display.setCursor(graphX + 2, y - 3);
        _display.print(yLabelValues[i]);
        _display.print("%");
    }
    
    int maxValue = 400;
    int minValue = 100;
    int valueRange = maxValue - minValue;
    
    // Tegn vækstgrafen med et punkt for hver time
    for (int i = 0; i < numPoints - 1; i++) {
        // Beregn koordinater for timebaserede datapunkter
        int x1 = xLabelStartX + (i * hourWidth);
        int y1 = graphY + graphHeight - ((growthData[i] - minValue) * graphHeight / valueRange);
        int x2 = xLabelStartX + ((i+1) * hourWidth);
        int y2 = graphY + graphHeight - ((growthData[i+1] - minValue) * graphHeight / valueRange);
        
        // Tegn linje mellem punkter
        _display.drawLine(x1, y1, x2, y2, COLOR_BLACK);
        
        // Marker timepunkter med små rektangler (kun ved lige timer)
        if (i % 2 == 0) {
            _display.fillRect(x1-2, y1-2, 4, 4, COLOR_BLACK);
        }
    }
    
    // Tegn en mere detaljeret graf med alle detaljerede data
    float pointSpacing = (float)hourWidth / 6; // 6 punkter per time
    
    for (int i = 0; i < data.growthDataSize - 1; i++) {
        int x1 = xLabelStartX + (i * pointSpacing);
        int y1 = graphY + graphHeight - ((data.growthData[i] - minValue) * graphHeight / valueRange);
        int x2 = xLabelStartX + ((i+1) * pointSpacing);
        int y2 = graphY + graphHeight - ((data.growthData[i+1] - minValue) * graphHeight / valueRange);
        
        // Tegn tyndere linjer for de detaljerede data
        _display.drawLine(x1, y1, x2, y2, COLOR_BLACK);
    }
    
    // Marker peak-værdien med en rød markør
    int peakIndex = 0;
    int peakValue = 0;
    
    for (int i = 0; i < data.growthDataSize; i++) {
        if (data.growthData[i] > peakValue) {
            peakValue = data.growthData[i];
            peakIndex = i;
        }
    }
    
    int peakX = xLabelStartX + (peakIndex * pointSpacing);
    int peakY = graphY + graphHeight - ((peakValue - minValue) * graphHeight / valueRange);
    _display.fillRect(peakX-3, peakY-3, 6, 6, COLOR_RED);
}
