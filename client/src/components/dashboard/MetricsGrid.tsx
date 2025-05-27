import React from 'react';
import { Droplet, Thermometer, TrendingUp } from 'lucide-react';
import { MetricCard } from './MetricCard';

interface MetricsGridProps {
    currentReading: any;
}

export const MetricsGrid: React.FC<MetricsGridProps> = ({
                                                            currentReading
                                                        }) => {

    const temperature = currentReading?.temperature ?? null;
    const humidity = currentReading?.humidity ?? null;
    const rise = currentReading?.rise ?? null;

    const hasData = currentReading !== null;

    return (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12">
            <MetricCard
                title="Temperature"
                value={temperature}
                unit="°C"
                icon={Thermometer}
                isLoading={false}
                showEmptyState={!hasData}
            />
            <MetricCard
                title="Humidity"
                value={humidity}
                unit="%"
                icon={Droplet}
                isLoading={false}
                showEmptyState={!hasData}
            />
            <MetricCard
                title="Growth"
                value={rise}
                unit="%"
                icon={TrendingUp}
                isLoading={false}
                showEmptyState={!hasData}
            />
        </div>
    );
};