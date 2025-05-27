import React, {useCallback, useEffect, useMemo, useState} from 'react';
import {Card, CardContent,} from '@/components/ui/card';
import {AlertCircle, PlusCircle} from 'lucide-react';
import SourdoughManager from '@/components/analyzer/SourdoughManager';
import {useWebSocket} from '@/hooks/useWebsocket';
import {useOptimizedAnalyzerData} from '@/hooks/useOptimizedAnalyzerData';
import {useAtomValue} from 'jotai';
import {analyzersAtom, userInfoAtom} from '@/atoms';
import {Broadcasts, SourdoughReading} from '@/api/websocket-client';
import {DashboardHeader, MetricsGrid, SourdoughChart,} from '@/components/dashboard';
import {getTimeRangeInMs, TimeRange,} from '@/helpers/dashboardUtils';
import {Button} from "@/components";
import ActivationForm from '@/components/analyzer/ActivationForm';
import { Dialog, DialogContent, DialogTitle, DialogDescription } from '@/components/ui/dialog';

const HomeDashboard: React.FC = () => {
    const [timeRange, setTimeRange] = useState<TimeRange>("12h");
    const [selectedAnalyzerId, setSelectedAnalyzerId] = useState<string>('');
    const [realTimeReading, setRealTimeReading] = useState<SourdoughReading | null>(null);

    const analyzers = useAtomValue(analyzersAtom);
    const user = useAtomValue(userInfoAtom);
    const { client, connected } = useWebSocket();
    const [activationDialogOpen, setActivationDialogOpen] = useState(false);

    const {
        readings: allReadings,
        latestReading,
        loading,
        error,
        refetch,
    } = useOptimizedAnalyzerData(selectedAnalyzerId);

    // Filter readings based on time range
    const readings = useMemo(() => {
        if (!allReadings?.length) return [];

        const now = new Date();
        const timeRangeMs = getTimeRangeInMs(timeRange);
        const cutoff = new Date(now.getTime() - timeRangeMs);

        return allReadings.filter(reading =>
            new Date(reading.timestamp) >= cutoff
        );
    }, [allReadings, timeRange]);

    // Auto-select first analyzer
    useEffect(() => {
        if (analyzers.length > 0 && !selectedAnalyzerId) {
            setSelectedAnalyzerId(analyzers[0].id);
        }
    }, [analyzers, selectedAnalyzerId]);

    // WebSocket subscription
    useEffect(() => {
        if (!client || !connected || !user?.userId) return;

        const subscribeToData = async () => {
            try {
                await client.send.sourdoughData({ userId: user.userId });
            } catch (err) {
                console.error('Failed to subscribe to WebSocket:', err);
            }
        };

        subscribeToData();

        return client.on(Broadcasts.sourdoughReading, (payload: SourdoughReading) => {
            setRealTimeReading(payload);
        });
    }, [client, connected, user?.userId]);

    // Event handlers
    const handleTimeRangeChange = useCallback((newTimeRange: string) => {
        setTimeRange(newTimeRange as TimeRange);
    }, []);

    const handleQuickRefresh = useCallback(async () => {
        setRealTimeReading(null);

        if (client && connected && user?.userId) {
            try {
                await client.send.sourdoughData({ userId: user.userId });
            } catch (err) {
                console.error('Quick refresh failed:', err);
            }
        }
    }, [client, connected, user?.userId]);

    const handleDeepRefresh = useCallback(() => {
        setRealTimeReading(null);
        refetch();
    }, [refetch]);

    const handleAnalyzerChange = useCallback((analyzerId: string) => {
        setSelectedAnalyzerId(analyzerId);
        setRealTimeReading(null);
    }, []);

    const currentReading = useMemo(() =>
            realTimeReading || latestReading,
        [realTimeReading, latestReading]
    );

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
                            onRefresh={handleDeepRefresh}
                            onQuickRefresh={handleQuickRefresh}
                            loading={loading}
                            currentReading={currentReading}
                            realTimeReading={realTimeReading}
                            isDataStale={false}
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

                        <MetricsGrid realTimeReading={realTimeReading} />

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