import React, { useState } from 'react';
import { RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { AnalyzerSelector } from '@/components/analyzer/AnalyzerSelector';

interface DashboardHeaderProps {
    selectedAnalyzerId: string;
    onAnalyzerChange: (id: string) => void;
    onRefresh: () => void;
    onQuickRefresh?: () => void;
    loading: boolean;
    currentReading?: any;
    realTimeReading?: any;
    isDataStale?: boolean;
}

export const DashboardHeader: React.FC<DashboardHeaderProps> = ({
                                                                    selectedAnalyzerId,
                                                                    onAnalyzerChange,
                                                                    onRefresh,
                                                                    onQuickRefresh,
                                                                    loading,
                                                                    currentReading,
                                                                    realTimeReading,
                                                                    isDataStale
                                                                }) => {
    const [quickRefreshing, setQuickRefreshing] = useState(false);

    const handleQuickRefresh = async () => {
        if (!onQuickRefresh) {
            onRefresh();
            return;
        }

        setQuickRefreshing(true);
        try {
            await onQuickRefresh();
        } finally {
            setTimeout(() => setQuickRefreshing(false), 300);
        }
    };

    const hasLiveData = !!realTimeReading;
    const isRefreshing = loading || quickRefreshing;

    return (
        <div className="flex justify-between items-center mb-6">
            <h1 className="text-3xl font-bold text-accent-foreground p-2 rounded-md">
                My Sourdough
            </h1>
            <div className="flex items-center gap-4">
                {/* Analyzer Selector */}
                <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-accent-foreground">Analyzer:</span>
                    <AnalyzerSelector
                        selectedAnalyzerId={selectedAnalyzerId}
                        onAnalyzerChange={onAnalyzerChange}
                        className="w-48"
                    />
                </div>

                <Button
                    variant="outline"
                    size="sm"
                    onClick={handleQuickRefresh}
                    disabled={isRefreshing || !selectedAnalyzerId}
                    className="border-border-brown"
                    title={onQuickRefresh ? "Quick refresh (WebSocket only)" : "Refresh data"}
                >
                    <RefreshCw className={`h-4 w-4 mr-2 ${isRefreshing ? 'animate-spin' : ''}`} />
                    {quickRefreshing ? 'Refreshing...' : 'Refresh'}
                </Button>

                {onQuickRefresh && (
                    <Button
                        variant="ghost"
                        size="sm"
                        onClick={onRefresh}
                        disabled={loading || !selectedAnalyzerId}
                        className="text-xs px-2"
                        title="Full refresh (reload all data)"
                    >
                        <RefreshCw className={`h-3 w-3 mr-1 ${loading ? 'animate-spin' : ''}`} />
                        Deep
                    </Button>
                )}

                {currentReading && (
                    <div className="text-right">
                        <div className="text-sm text-accent-foreground/60 flex items-center gap-1">
                            Last updated
                            
                        </div>
                        <div className="text-sm font-medium text-accent-foreground">
                            {new Date(currentReading.localTime || currentReading.timestamp).toLocaleString()}
                        </div>
                        {isDataStale && !hasLiveData && (
                            <div className="text-xs text-yellow-600 font-medium">
                                ⚠️ Data is stale
                            </div>
                        )}
                        
                    </div>
                )}

                {!currentReading && (
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