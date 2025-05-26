import { useState, useEffect, useCallback } from 'react';
import { useAtom } from 'jotai';
import { Activity } from 'lucide-react';
import { Card } from '../ui/card';
import { useWebSocket } from '@/hooks';
import { Broadcasts, DiagnosticsResponse } from '@/api/websocket-client.ts';
import { diagnosticsAtom } from '@/atoms/diagnosticsAtom.ts';
import { api } from '@/hooks';
import { AdminAnalyzerListResponse } from '@/api/Api.ts';

const formatUptime = (uptimeMs: number) => {
    const seconds = Math.floor(uptimeMs / 1000);
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    return `${hours}h ${minutes}m ${secs}s`;
};

const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
};

const formatTimestamp = (timestamp: string) => {
    return new Date(timestamp).toLocaleString();
};

const getStateColor = (state: string) => {
    return state === 'active' || state === 'running'
        ? 'bg-green-100 text-green-800 dark:bg-slate-800 dark:text-green-400'
        : 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-400';
};

const DiagnosticHeader = ({ diagnostic, analyzerName }: {
    diagnostic: DiagnosticsResponse;
    analyzerName: string;
}) => (
    <div className="flex items-start justify-between">
        <div className="flex-1">
            <div className="flex items-center gap-3 mb-2">
                <Activity className="w-5 h-5" />
                <h3 className="text-lg font-semibold">{analyzerName}</h3>
                <span className={`text-xs px-2 py-1 rounded-full font-medium ${getStateColor(diagnostic.state)}`}>
                    {diagnostic.state}
                </span>
            </div>
            <div className="mb-4">
                <p className="text-xs text-muted-foreground font-mono">{diagnostic.analyzerId}</p>
            </div>
        </div>
    </div>
);

const DiagnosticField = ({ label, value }: { label: string; value: string }) => (
    <div>
        <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">{label}</label>
        <p className="mt-1">{value}</p>
    </div>
);

const DiagnosticGrid = ({ diagnostic }: { diagnostic: DiagnosticsResponse }) => (
    <>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 text-sm">
            <DiagnosticField label="Uptime" value={formatUptime(diagnostic.uptime)} />
            <DiagnosticField label="Free Heap" value={formatBytes(diagnostic.freeHeap)} />
            <DiagnosticField
                label="WiFi Status"
                value={diagnostic.wifi?.connected ? `Connected (${diagnostic.wifi.rssi} dBm)` : 'Disconnected'}
            />
            <DiagnosticField label="Last Updated" value={formatTimestamp(diagnostic.timestamp)} />
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 text-sm">
            <DiagnosticField label="Temperature" value={`${diagnostic.sensors?.temperature?.toFixed(1) || 'N/A'}°C`} />
            <DiagnosticField label="Humidity (Sensor)" value={`${diagnostic.sensors?.humidity?.toFixed(1) || 'N/A'}%`} />
            <DiagnosticField label="Rise" value={`${diagnostic.sensors?.rise?.toFixed(1) || 'N/A'}%`} />
            <DiagnosticField label="Humidity (Main)" value={`${diagnostic.humidity?.toFixed(1) || 'N/A'}%`} />
        </div>
    </>
);

export function DiagnosticsAdmin() {
    const [diagnostics, setDiagnostics] = useAtom(diagnosticsAtom);
    const [analyzers, setAnalyzers] = useState<AdminAnalyzerListResponse[]>([]);
    const [loading, setLoading] = useState(diagnostics.length === 0);
    const { client, connected } = useWebSocket();

    // Memoiseret opslag af analysatornavn for hurtigere adgang
    const getAnalyzerName = useCallback((analyzerId: string) => {
        const analyzer = analyzers.find(a => a.id === analyzerId);
        return analyzer?.name || `Analyzer ${analyzerId.slice(0, 8)}...`;
    }, [analyzers]);

    // Memoiseret opdatering af diagnostikdata for effektiv statehåndtering
    const handleDiagnosticsData = useCallback((diagnosticsData: DiagnosticsResponse) => {
        console.log('Received diagnostics data:', diagnosticsData);
        setDiagnostics((prev: DiagnosticsResponse[]) => {
            const existingIndex = prev.findIndex(d => d.analyzerId === diagnosticsData.analyzerId);

            if (existingIndex >= 0) {
                const updated = [...prev];
                updated[existingIndex] = diagnosticsData;
                return updated;
            }
            return [...prev, diagnosticsData];
        });
        setLoading(false);
    }, [setDiagnostics]);

    useEffect(() => {
        const fetchAnalyzers = async () => {
            try {
                const response = await api.analyzer.getAllAnalyzers();
                setAnalyzers(response.data || []);
            } catch (error) {
                console.error('Failed to fetch analyzers:', error);
            }
        };
        fetchAnalyzers();
    }, []);

    useEffect(() => {
        if (!connected || !client) {
            setLoading(false);
            return;
        }

        let cleanup: (() => void) | undefined;

        const setupDiagnostics = async () => {
            try {
                if (diagnostics.length === 0) setLoading(true);

                cleanup = client.on(Broadcasts.diagnosticsResponse, handleDiagnosticsData);

                await new Promise(resolve => setTimeout(resolve, 100));
                await client.send.diagnostics({ userId: "" });

                setTimeout(() => setLoading(false), 5000);
            } catch (error) {
                console.error('Failed to setup diagnostics:', error);
                setLoading(false);
            }
        };

        setupDiagnostics();
        return () => cleanup?.();
    }, [connected, client, diagnostics.length, handleDiagnosticsData]);

    if (loading) {
        return (
            <div className="space-y-6">
                <div>
                    <h2 className="text-2xl font-bold">Analyzer Diagnostics</h2>
                    <p className="text-muted-foreground">Real-time monitoring of analyzer health and status</p>
                </div>
                <div className="text-center py-8">Loading diagnostics...</div>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            <div>
                <h2 className="text-2xl font-bold">Analyzer Diagnostics</h2>
                <p className="text-muted-foreground">Real-time monitoring of analyzer health and status</p>
            </div>

            <div className="grid gap-4">
                {diagnostics.length === 0 ? (
                    <Card className="p-8 text-center">
                        <p className="text-muted-foreground">No diagnostics data available</p>
                    </Card>
                ) : (
                    diagnostics.map((diagnostic) => (
                        <Card key={diagnostic.analyzerId} className="p-6 border-border shadow-md">
                            <div className="space-y-4">
                                <DiagnosticHeader
                                    diagnostic={diagnostic}
                                    analyzerName={getAnalyzerName(diagnostic.analyzerId)}
                                />
                                <DiagnosticGrid diagnostic={diagnostic} />
                            </div>
                        </Card>
                    ))
                )}
            </div>
        </div>
    );
}