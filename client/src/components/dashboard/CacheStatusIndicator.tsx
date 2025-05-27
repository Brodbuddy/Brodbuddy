import React from 'react';
import { Activity } from 'lucide-react';

type TimeRange = '1h' | '6h' | '12h';

interface CacheStatusProps {
    cacheStatus?: 'hit' | 'miss' | 'partial';
    isDataStale?: boolean;
    readingsCount: number;
    allReadingsCount?: number;
    timeRange: TimeRange;
}

export const CacheStatusIndicator: React.FC<CacheStatusProps> = ({
                                                                     cacheStatus = 'miss',
                                                                     isDataStale = false,
                                                                     readingsCount,
                                                                     allReadingsCount,
                                                                     timeRange
                                                                 }) => {
    if (process.env.NODE_ENV !== 'development') return null;

    const statusColors = {
        hit: 'text-green-600',
        partial: 'text-yellow-600',
        miss: 'text-red-600'
    };

    const efficiency = allReadingsCount && allReadingsCount > 0 ?
        ((readingsCount / allReadingsCount) * 100).toFixed(1) : '0';

    return (
        <div className="mb-2 p-2 bg-gray-100 dark:bg-gray-800 rounded text-xs text-muted-foreground border">
            <div className="flex items-center gap-4 flex-wrap">
                <span className="flex items-center gap-1">
                    <Activity className="h-3 w-3" />
                    Cache: <span className={statusColors[cacheStatus]}>{cacheStatus.toUpperCase()}</span>
                </span>
                <span>Data: {isDataStale ? '🔴 stale' : '🟢 fresh'}</span>
                <span>Range: {timeRange}</span>
                <span>Shown: {readingsCount}</span>
                {allReadingsCount && (
                    <span>Total: {allReadingsCount} ({efficiency}% filtered)</span>
                )}
            </div>
        </div>
    );
};
