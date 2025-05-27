import React from 'react';
import { Droplet, Thermometer, TrendingUp } from 'lucide-react';
import { MetricCard } from './MetricCard';

interface MetricsGridProps {
    realTimeReading: any;
}

export const MetricsGrid: React.FC<MetricsGridProps> = ({
                                                            realTimeReading
                                                        }) => {

    const temperature = realTimeReading?.temperature || null;
    const humidity = realTimeReading?.humidity || null;
    const rise = realTimeReading?.rise || null;

    const hasData = realTimeReading !== null;

    return (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12">
            <MetricCard
                title="Temperature"
                value={temperature}
                unit="°C"
                icon={Thermometer}
                isLoading={false}
                freshness="fresh"
                showEmptyState={!hasData}
            />
            <MetricCard
                title="Humidity"
                value={humidity}
                unit="%"
                icon={Droplet}
                isLoading={false}
                freshness="fresh"

                showEmptyState={!hasData}
            />
            <MetricCard
                title="Growth"
                value={rise}
                unit="%"
                icon={TrendingUp}
                isLoading={false}
                freshness="fresh"
                showEmptyState={!hasData}
            />
        </div>
    );
};