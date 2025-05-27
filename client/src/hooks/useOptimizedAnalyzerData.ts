import { useState, useEffect, useCallback } from 'react';
import { api } from './useHttp';
import { AnalyzerReading } from '../api/Api';

interface ProcessedReading {
    date: string;
    temperature: number;
    humidity: number;
    rise: number;
    timestamp: string;
    localTime: string;
}

const memoryCache = new Map<string, {
    data: ProcessedReading[];
    timestamp: number;
    latestReading: ProcessedReading | null;
}>();

const CACHE_DURATION = 5 * 60 * 1000;
const MAX_CACHE_READINGS = 500;

export const useOptimizedAnalyzerData = (analyzerId?: string) => {
    const [readings, setReadings] = useState<ProcessedReading[]>([]);
    const [latestReading, setLatestReading] = useState<ProcessedReading | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const processReadings = useCallback((rawReadings: AnalyzerReading[]): ProcessedReading[] => {
        return rawReadings
            .filter(reading => reading.temperature !== null && reading.humidity !== null && reading.rise !== null)
            .map(reading => ({
                date: reading.localTime,
                temperature: reading.temperature || 0,
                humidity: reading.humidity || 0,
                rise: reading.rise || 0,
                timestamp: reading.timestamp,
                localTime: reading.localTime
            }))
            .sort((a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime())
            .slice(-MAX_CACHE_READINGS);
    }, []);

    const getCachedData = useCallback((analyzerIdToCheck: string) => {
        const cached = memoryCache.get(analyzerIdToCheck);
        if (!cached) return null;

        const now = Date.now();
        const ageMs = now - cached.timestamp;

        if (ageMs > CACHE_DURATION) {
            memoryCache.delete(analyzerIdToCheck);
            return null;
        }

        return cached;
    }, []);

    const setCachedData = useCallback((analyzerIdToCache: string, data: ProcessedReading[], latest: ProcessedReading | null) => {
        memoryCache.set(analyzerIdToCache, {
            data: data.slice(-MAX_CACHE_READINGS),
            timestamp: Date.now(),
            latestReading: latest
        });
    }, []);

    const fetchData = useCallback(async (analyzerIdToFetch: string) => {
        if (!analyzerIdToFetch) {
            setLoading(false);
            setError(null);
            return;
        }


        const cached = getCachedData(analyzerIdToFetch);
        if (cached) {
            console.log(`📦 Using cached data for ${analyzerIdToFetch} (${cached.data.length} readings)`);
            setReadings(cached.data);
            setLatestReading(cached.latestReading);
            return;
        }

        setLoading(true);
        setError(null);

        try {
            console.log(`🔍 Fetching fresh data for ${analyzerIdToFetch}...`);

            const [latestResponse, readingsResponse] = await Promise.all([
                api.analyzerreadings.getLatestReading(analyzerIdToFetch),
                api.analyzerreadings.getReadingsForCache(analyzerIdToFetch, { maxResults: MAX_CACHE_READINGS })
            ]);

            const processedReadings = processReadings(readingsResponse.data);
            const processedLatest = processReadings([latestResponse.data])[0] || null;

            setReadings(processedReadings);
            setLatestReading(processedLatest);

            setCachedData(analyzerIdToFetch, processedReadings, processedLatest);

        } catch (err: any) {
            console.error('Failed to fetch analyzer data:', err);
            setError(err.response?.data?.message || 'Failed to fetch data');
        } finally {
            setLoading(false);
        }
    }, [processReadings, getCachedData, setCachedData]);

    const refetch = useCallback(() => {
        if (!analyzerId) return;
        memoryCache.delete(analyzerId);
        fetchData(analyzerId);
    }, [analyzerId, fetchData]);

    const clearCache = useCallback(() => {
        if (analyzerId) {
            memoryCache.delete(analyzerId);
        }
    }, [analyzerId]);

    useEffect(() => {
        if (analyzerId) {
            fetchData(analyzerId);
        } else {
            setReadings([]);
            setLatestReading(null);
            setError(null);
        }
    }, [analyzerId, fetchData]);

    return {
        readings,
        latestReading,
        loading,
        error,
        refetch,
        clearCache,
        isDataStale: false,
        cacheStatus: getCachedData(analyzerId || '') ? 'hit' : 'miss'
    };
};
