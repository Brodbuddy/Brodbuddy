export type TimeRange = '1h' | '6h' | '12h';

export const getTimeRangeInMs = (range: TimeRange): number => {
    switch (range) {
        case '1h': return 60 * 60 * 1000;
        case '6h': return 6 * 60 * 60 * 1000;
        case '12h': return 12 * 60 * 60 * 1000;
        default: return 12 * 60 * 60 * 1000;
    }
};

export const getDataFreshness = (reading: any): 'fresh' | 'stale' | 'old' => {
    if (!reading) return 'old';

    const now = Date.now();
    const readingTime = new Date(reading.timestamp || reading.localTime).getTime();
    const ageMinutes = (now - readingTime) / (1000 * 60);

    if (ageMinutes < 5) return 'fresh';
    if (ageMinutes < 30) return 'stale';
    return 'old';
};