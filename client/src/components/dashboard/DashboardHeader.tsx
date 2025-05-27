import React from 'react';
import { Download, Loader2, WifiOff, Wifi } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { AnalyzerSelector } from '@/components/analyzer/AnalyzerSelector';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { format } from 'date-fns';
import { useWebSocket } from '@/hooks/useWebsocket';

interface DashboardHeaderProps {
    selectedAnalyzerId: string;
    onAnalyzerChange: (id: string) => void;
    loading: boolean;
    currentReading?: any;
    selectedAnalyzer?: any;
    firmwareVersions?: any[];
    onStartOtaUpdate?: (userId: string, analyzerId: string, firmwareVersionId: string) => void;
    otaProgress?: Record<string, number>;
    isUpdating?: boolean;
    userId?: string;
}

export const DashboardHeader: React.FC<DashboardHeaderProps> = ({
    selectedAnalyzerId,
    onAnalyzerChange,
    loading,
    currentReading,
    selectedAnalyzer,
    firmwareVersions = [],
    onStartOtaUpdate,
    otaProgress = {},
    isUpdating = false,
    userId
}) => {
    const { connected } = useWebSocket();
    const currentProgress = otaProgress[selectedAnalyzerId];
    const hasUpdate = selectedAnalyzer?.hasUpdate;
    const currentVersion = selectedAnalyzer?.firmwareVersion;

    const availableUpdates = firmwareVersions.filter(v => 
        v.version !== currentVersion
    );

    const handleFirmwareUpdate = (versionId: string) => {
        if (onStartOtaUpdate && selectedAnalyzerId && userId) {
            onStartOtaUpdate(userId, selectedAnalyzerId, versionId);
        }
    };

    return (
        <div className="flex justify-between items-center mb-6">
            <h1 className="text-3xl font-bold text-accent-foreground p-2 rounded-md">
                My Sourdough
            </h1>
            <div className="flex items-center gap-4">
                <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-accent-foreground">Analyzer:</span>
                    <AnalyzerSelector
                        selectedAnalyzerId={selectedAnalyzerId}
                        onAnalyzerChange={onAnalyzerChange}
                        className="w-48"
                    />
                </div>

                {selectedAnalyzer?.firmwareVersion && (
                    <span className="text-sm text-muted-foreground">
                        Firmware: {selectedAnalyzer.firmwareVersion}
                    </span>
                )}

                {hasUpdate && !isUpdating && (
                    <Button 
                        size="sm" 
                        onClick={() => {
                            const latestStable = firmwareVersions.find(f => f.isStable);
                            if (latestStable && onStartOtaUpdate && userId) {
                                onStartOtaUpdate(userId, selectedAnalyzerId, latestStable.id);
                            }
                        }}
                        className="bg-accent-foreground text-primary-foreground hover:bg-accent-foreground/90"
                    >
                        <Download className="mr-1.5 h-3 w-3" />
                        Update Firmware
                    </Button>
                )}

                {isUpdating && (
                    <div className="flex items-center gap-2">
                        <Loader2 className="h-4 w-4 animate-spin" />
                        <span className="text-sm">
                            {currentProgress !== undefined 
                                ? `Updating: ${currentProgress}%` 
                                : 'Starting update...'}
                        </span>
                    </div>
                )}

                <div className="flex items-center gap-2">
                    {connected ? (
                        <Wifi className="h-4 w-4 text-green-600" />
                    ) : (
                        <WifiOff className="h-4 w-4 text-red-600" />
                    )}
                    <span className="text-sm text-accent-foreground/60">
                        {connected ? 'Connected' : 'Disconnected'}
                    </span>
                </div>

                {currentReading && (
                    <div className="text-right">
                        <div className="text-sm text-accent-foreground/60">
                            Last updated
                        </div>
                        <div className="text-sm font-medium text-accent-foreground">
                            {format(new Date(currentReading.localTime || currentReading.timestamp), 'dd/MM/yyyy, HH:mm:ss')}
                        </div>
                    </div>
                )}

                {!currentReading && !loading && (
                    <div className="text-right">
                        <div className="text-sm text-accent-foreground/60">Status</div>
                        <div className="text-sm text-orange-600">
                            Waiting for data...
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};