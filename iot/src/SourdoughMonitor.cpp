#include "SourdoughMonitor.h"

SourdoughMonitor::SourdoughMonitor(SourdoughDisplay &display) : _display(display) {}

SourdoughData SourdoughMonitor::generateMockData()
{
    const int mockGrowthValues[] = {
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
        277, 268, 260, 255, 250, 245};
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

    // Beregn start-tidsstempel (12 timer før nu)
    unsigned long now = millis() / 1000;
    unsigned long startTime = now - (12 * 60 * 60);

    // Tilføj mockdata med 10-minutters intervaller
    unsigned long interval = 10 * 60;
    for (int i = 0; i < mockDataSize; i++)
    {
        unsigned long timestamp = startTime + (i * interval);
        addDataPoint(data, mockGrowthValues[i], timestamp);
    }

    return data;
}

void SourdoughMonitor::addDataPoint(SourdoughData &data, int growthPercentage, unsigned long timestamp)
{
    int insertIndex;

    if (data.bufferFull)
    {
        insertIndex = data.oldestIndex;
        data.oldestIndex = (data.oldestIndex + 1) % MAX_DATA_POINTS;
    }
    else
    {
        insertIndex = data.dataCount;
        data.dataCount++;

        if (data.dataCount >= MAX_DATA_POINTS)
        {
            data.bufferFull = true;
            data.dataCount = MAX_DATA_POINTS;
        }
    }

    data.growthValues[insertIndex] = growthPercentage;
    data.timestamps[insertIndex] = timestamp;

    data.currentGrowth = growthPercentage;

    updatePeakInfo(data);
}

void SourdoughMonitor::updatePeakInfo(SourdoughData &data)
{
    data.peakGrowth = 0;
    int peakIndex = -1;

    // Find peak værdi og indeks
    for (int i = 0; i < data.dataCount; i++)
    {
        int actualIndex = (data.oldestIndex + i) % MAX_DATA_POINTS;
        if (data.growthValues[actualIndex] > data.peakGrowth)
        {
            data.peakGrowth = data.growthValues[actualIndex];
            peakIndex = actualIndex;
        }
    }

    // Beregn timer siden peak
    if (peakIndex >= 0)
    {
        unsigned long now = millis() / 1000;
        unsigned long peakTime = data.timestamps[peakIndex];
        data.peakHoursAgo = (now - peakTime) / 3600.0; // Konverter sekunder til timer
    }
    else
    {
        data.peakHoursAgo = 0;
    }
}

void SourdoughMonitor::updateDisplay(const SourdoughData &data)
{
    _display.clearBuffers();

    // Tegn linje til at adskille header fra grafområdet
    _display.drawLine(2, 35, _display.width() - 3, 35, COLOR_BLACK);

    drawHeader(data);
    drawBattery(data.batteryLevel);
    drawGraph(data);

    _display.updateDisplay();
}

void SourdoughMonitor::drawHeader(const SourdoughData &data)
{
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

void SourdoughMonitor::drawBattery(int batteryLevel)
{
    // Tegn lodret batteri form
    _display.drawRect(270, 5, 10, 20, COLOR_BLACK); // Batteriets ydre
    _display.drawRect(272, 2, 6, 3, COLOR_BLACK);   // Batteriets top
    // Fyld batteriniveau fra bunden op
    int fillHeight = (int)(batteryLevel / 100.0 * 16);
    _display.fillRect(272, 23 - fillHeight, 6, fillHeight, COLOR_BLACK);
}

void SourdoughMonitor::drawGraph(const SourdoughData &data)
{
    // Grafområdets dimensioner
    int graphX = 10;
    int graphY = 45;
    int graphWidth = _display.width() - 20;
    int graphHeight = _display.height() - graphY - 10;

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

    for (int hour = 0; hour <= 12; hour++)
    {
        int x = xLabelStartX + (hour * hourWidth);

        // Tegn gitterlinje for hver time
        _display.drawLine(x, graphY, x, graphY + graphHeight, COLOR_BLACK);

        // Tilføj etiketter kun for lige timer
        if (hour % 2 == 0)
        {
            int labelIndex = hour / 2;
            if (labelIndex < sizeof(timeLabels) / sizeof(timeLabels[0]))
            {
                _display.setCursor(x - 8, graphY + graphHeight + 2);
                _display.setTextColor(COLOR_BLACK);
                _display.print(timeLabels[labelIndex]);
            }
        }
    }

    // Tegn Y-aksens etiketter (vækstprocent)
    int yLabelValues[] = {150, 200, 250, 300, 350, 400};

    int numYLabels = sizeof(yLabelValues) / sizeof(yLabelValues[0]);
    for (int i = 0; i < numYLabels; i++)
    {
        int y = graphY + graphHeight - (i + 1) * graphHeight / numYLabels;

        // Tegn vandret gitterlinje
        _display.drawLine(xLabelStartX, y, graphX + graphWidth, y, COLOR_BLACK);

        // Tegn etiket
        _display.setCursor(graphX + 2, y - 3);
        _display.print(yLabelValues[i]);
        _display.print("%");
    }

    // Værdier for skalering
    int maxValue = 400;
    int minValue = 100;
    int valueRange = maxValue - minValue;

    if (data.dataCount == 0)
        return;

    // Beregn tidsramme (12 timer total)
    unsigned long now = millis() / 1000;
    unsigned long twelveHoursAgo = now - (12 * 60 * 60);
    float timeRange = 12.0 * 60 * 60; // 12 timer i sekunder

    // Kun tegn punkter, hvis vi har mindst 2
    if (data.dataCount >= 2)
    {
        // Tegn linjerne mellem punkter
        for (int i = 0; i < data.dataCount - 1; i++)
        {
            // Beregn de faktiske indekser med hensyn til cirkulær buffer
            int idx1 = (data.oldestIndex + i) % MAX_DATA_POINTS;
            int idx2 = (data.oldestIndex + i + 1) % MAX_DATA_POINTS;

            // Beregn relative positioner på tidsaksen
            float timePct1 = (data.timestamps[idx1] - twelveHoursAgo) / timeRange;
            float timePct2 = (data.timestamps[idx2] - twelveHoursAgo) / timeRange;

            // Begræns til gyldig tidsskala (0.0 til 1.0)
            timePct1 = max(0.0f, min(1.0f, timePct1));
            timePct2 = max(0.0f, min(1.0f, timePct2));

            // Beregn x-koordinater baseret på disse tidspositioner
            int x1 = xLabelStartX + (timePct1 * (graphWidth - yLabelWidth));
            int x2 = xLabelStartX + (timePct2 * (graphWidth - yLabelWidth));

            // Beregn y-koordinater baseret på vækstværdier
            int y1 = graphY + graphHeight - ((data.growthValues[idx1] - minValue) * graphHeight / valueRange);
            int y2 = graphY + graphHeight - ((data.growthValues[idx2] - minValue) * graphHeight / valueRange);

            // Tegn linje mellem punkter
            _display.drawLine(x1, y1, x2, y2, COLOR_BLACK);

            // Marker hver 3. datapunkt
            if (i % 3 == 0)
            {
                _display.fillRect(x1 - 2, y1 - 2, 4, 4, COLOR_BLACK);
            }
        }
    }

    // Find peak for at markere den
    int peakValue = 0;
    int peakIndex = -1;

    for (int i = 0; i < data.dataCount; i++)
    {
        int idx = (data.oldestIndex + i) % MAX_DATA_POINTS;
        if (data.growthValues[idx] > peakValue)
        {
            peakValue = data.growthValues[idx];
            peakIndex = idx;
        }
    }

    // Hvis vi fandt en peak, marker den
    if (peakIndex >= 0)
    {
        float peakTimePct = (data.timestamps[peakIndex] - twelveHoursAgo) / timeRange;
        peakTimePct = max(0.0f, min(1.0f, peakTimePct));

        int peakX = xLabelStartX + (peakTimePct * (graphWidth - yLabelWidth));
        int peakY = graphY + graphHeight - ((peakValue - minValue) * graphHeight / valueRange);

        _display.fillRect(peakX - 3, peakY - 3, 6, 6, COLOR_RED);
    }
}