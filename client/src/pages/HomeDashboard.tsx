import React, {useCallback, useEffect, useMemo, useState} from 'react';
import {Card, CardContent,} from '@/components/ui/card';
import {AlertCircle, PlusCircle} from 'lucide-react';
import SourdoughManager from '@/components/analyzer/SourdoughManager';
import {useWebSocket} from '@/hooks/useWebsocket';
import {useOptimizedAnalyzerData} from '@/hooks/useOptimizedAnalyzerData';
import {useAtomValue} from 'jotai';
import {getDefaultStore} from 'jotai';
import {analyzersAtom, userInfoAtom} from '@/atoms';
import {api} from '@/hooks/useHttp';
import {Broadcasts, SourdoughReading} from '@/api/websocket-client';
import {DashboardHeader, MetricsGrid, SourdoughChart,} from '@/components/dashboard';
import {getTimeRangeInMs, TimeRange,} from '@/helpers/dashboardUtils';
import {Button} from "@/components";
import ActivationForm from '@/components/analyzer/ActivationForm';
import { Dialog, DialogContent, DialogTitle, DialogDescription } from '@/components/ui/dialog';
import { useFirmwareVersions, useOtaSubscription } from '@/hooks';
import { toast } from 'sonner';

const HomeDashboard: React.FC = () => {
    const [timeRange, setTimeRange] = useState<TimeRange>("1h");
    const [selectedAnalyzerId, setSelectedAnalyzerId] = useState<string>('');
    const [chartData, setChartData] = useState<Array<{
        date: string;
        temperature: number;
        humidity: number;
        rise: number;
        timestamp: string;
        localTime: string;
    }>>([]);
    const [latestReading, setLatestReading] = useState<SourdoughReading | null>(null);

    const analyzers = useAtomValue(analyzersAtom);
    const user = useAtomValue(userInfoAtom);
    const { client, connected } = useWebSocket();
    const { firmwareVersions } = useFirmwareVersions();
    const [activationDialogOpen, setActivationDialogOpen] = useState(false);

    const {
        readings: historicalReadings,
        latestReading: cachedLatestReading,
        loading,
        error,
        refetch,
    } = useOptimizedAnalyzerData(selectedAnalyzerId);

    const analyzerIds = useMemo(() => analyzers.map(a => a.id), [analyzers]);
    
    const handleOtaComplete = useCallback(async () => {
        refetch();
        
        try {
            const response = await api.analyzer.getUserAnalyzers();
            console.log('[Dashboard] Refreshed analyzers after OTA complete:', response.data);
            const store = getDefaultStore();
            store.set(analyzersAtom, response.data);
        } catch (error) {
            console.error('Failed to refresh analyzers after OTA:', error);
        }
    }, [refetch]);
    
    const { otaProgress, updatingAnalyzers, startOtaUpdate } = useOtaSubscription(analyzerIds, handleOtaComplete);

    useEffect(() => {
        if (historicalReadings && historicalReadings.length > 0 && chartData.length === 0) {
            console.log('[Dashboard] Initializing chart with historical data:', historicalReadings.length, 'readings');
            setChartData(historicalReadings);
        }
    }, [historicalReadings, chartData.length]);

    useEffect(() => {
        if (cachedLatestReading && !latestReading) {
            setLatestReading({
                rise: cachedLatestReading.rise,
                temperature: cachedLatestReading.temperature,
                humidity: cachedLatestReading.humidity,
                epochTime: 0,
                timestamp: cachedLatestReading.timestamp,
                localTime: cachedLatestReading.localTime
            } as SourdoughReading);
        }
    }, [cachedLatestReading, latestReading]);

    const readings = useMemo(() => {
        if (!chartData?.length) return [];

        const now = new Date();
        const timeRangeMs = getTimeRangeInMs(timeRange);
        const cutoff = new Date(now.getTime() - timeRangeMs);

        const filtered = chartData.filter(reading =>
            new Date(reading.timestamp) >= cutoff
        );
        
        console.log('[Dashboard] Filtering readings for time range:', timeRange, 
            'Total:', chartData.length, 
            'Filtered:', filtered.length,
            'Latest rise in filtered:', filtered[filtered.length - 1]?.rise);
            
        return filtered;
    }, [chartData, timeRange]);

    useEffect(() => {
        if (analyzers.length > 0 && !selectedAnalyzerId) {
            setSelectedAnalyzerId(analyzers[0].id);
        }
    }, [analyzers, selectedAnalyzerId]);

    useEffect(() => {
        if (!client || !connected || !user?.userId) return;

        client.send.sourdoughData({ userId: user.userId }).catch(err => {
            console.error('Failed to subscribe to sourdough data:', err);
        });


        const unsubscribeSourdough = client.on(Broadcasts.sourdoughReading, (reading: SourdoughReading) => {
            console.log('[WebSocket] Received sourdough reading:', {
                rise: reading.rise,
                temperature: reading.temperature,
                humidity: reading.humidity,
                timestamp: reading.timestamp,
                localTime: reading.localTime
            });
            
            if (reading.rise < -100 || reading.rise > 500) {
                console.warn('[WebSocket] Suspicious rise value:', reading.rise);
            }
            
            toast.success('New data received', {
                description: `Temperature: ${reading.temperature.toFixed(1)}°C, Humidity: ${reading.humidity.toFixed(1)}%, Growth: ${reading.rise.toFixed(1)}%`,
                duration: 2000,
            });
            
            setLatestReading(reading);
            
            setChartData(prev => {
                const newDataPoint = {
                    date: reading.localTime,
                    temperature: reading.temperature,
                    humidity: reading.humidity,
                    rise: reading.rise,
                    timestamp: reading.timestamp,
                    localTime: reading.localTime
                };
                
                console.log('[WebSocket] Adding to chart data. Previous length:', prev.length);
                const updated = [...prev, newDataPoint];
                const result = updated.slice(-1000);
                console.log('[WebSocket] New chart data length:', result.length, 'Latest rise:', newDataPoint.rise);
                return result;
            });
        });

        return () => {
            unsubscribeSourdough();
        };
    }, [client, connected, user?.userId, refetch]);

    const handleTimeRangeChange = useCallback((newTimeRange: string) => {
        setTimeRange(newTimeRange as TimeRange);
        const rangeLabels: Record<TimeRange, string> = {
            '1h': '1 hour',
            '6h': '6 hours',
            '12h': '12 hours',
            '24h': '24 hours'
        };
        toast.info('Time range changed', {
            description: `Now showing data for the last ${rangeLabels[newTimeRange as TimeRange]}`,
            duration: 2000,
        });
    }, []);

    const handleAnalyzerChange = useCallback((analyzerId: string) => {
        setSelectedAnalyzerId(analyzerId);
    }, []);

    const selectedAnalyzer = useMemo(() => {
        const analyzer = analyzers.find(a => a.id === selectedAnalyzerId);
        console.log('[Dashboard] Selected analyzer:', analyzer);
        console.log('[Dashboard] Has update?', analyzer?.hasUpdate);
        console.log('[Dashboard] Firmware version:', analyzer?.firmwareVersion);
        return analyzer;
    }, [analyzers, selectedAnalyzerId]);

    // Hvis der ikke er nogen analyzers, vis kun SourdoughManager
    if (!analyzers || analyzers.length === 0) {
        return <SourdoughManager />;
    }

    return (
        <>
            <Card className="border-border-brown bg-bg-cream shadow-md mt-16">
                <div className="flex justify-end ">
                    <Button
                        className="bg-orange-500 text-black hover:bg-orange-600 dark:bg-orange-600 dark:text-white text-xs mr-6 px-2 py-0.5 h-7"
                        size="sm"
                        onClick={() => setActivationDialogOpen(true)}
                    >
                        <PlusCircle className="mr-1 h-3 w-3"/>
                        Add Device
                    </Button>
                </div>
                <CardContent className="p-4">
                    <div className="bg-bg-cream">
                        <DashboardHeader
                            selectedAnalyzerId={selectedAnalyzerId}
                            onAnalyzerChange={handleAnalyzerChange}
                            loading={loading}
                            currentReading={latestReading}
                            selectedAnalyzer={selectedAnalyzer}
                            firmwareVersions={firmwareVersions}
                            onStartOtaUpdate={startOtaUpdate}
                            otaProgress={otaProgress}
                            isUpdating={updatingAnalyzers.has(selectedAnalyzerId)}
                            userId={user?.userId}
                        />

                        {!connected && (
                            <div className="mb-6 p-4 bg-orange-50 border border-orange-200 rounded-lg text-orange-700 text-center">
                                🔌 WebSocket disconnected - using cached data
                            </div>
                        )}

                        {error && (
                            <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg flex items-center gap-2 text-red-700">
                                <AlertCircle className="h-5 w-5 shrink-0" />
                                <span>{error}</span>
                            </div>
                        )}

                        <MetricsGrid currentReading={latestReading} />

                        <div className="grid grid-cols-1 gap-6">
                            <SourdoughChart
                                readings={readings}
                                loading={loading}
                                timeRange={timeRange}
                                onTimeRangeChange={handleTimeRangeChange}
                                selectedAnalyzerId={selectedAnalyzerId}
                            />
                        </div>
                    </div>
                </CardContent>
            </Card>
            <Dialog open={activationDialogOpen} onOpenChange={setActivationDialogOpen}>
                <DialogContent className="sm:max-w-[425px]">
                    <DialogTitle>Add New Device</DialogTitle>
                    <DialogDescription>
                        Enter the activation code for your new device below.
                    </DialogDescription>
                    <ActivationForm
                        onSuccess={() => setActivationDialogOpen(false)}
                    />
                </DialogContent>
            </Dialog>
        </>
    );
};

export default HomeDashboard;