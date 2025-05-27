import React, { useMemo } from 'react';
import { RefreshCw, TrendingUp } from 'lucide-react';
import { Area, AreaChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { ChartContainer, ChartTooltip } from "@/components/ui/chart";
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";

type TimeRange = '1h' | '6h' | '12h' | '24h';

interface ProcessedReading {
    date: string;
    temperature: number;
    humidity: number;
    rise: number;
    timestamp: string;
    localTime: string;
}

interface SourdoughChartProps {
    readings: ProcessedReading[];
    loading: boolean;
    timeRange: TimeRange;
    onTimeRangeChange: (range: string) => void;
    selectedAnalyzerId: string;
}

export const SourdoughChart: React.FC<SourdoughChartProps> = ({
                                                                  readings,
                                                                  loading,
                                                                  timeRange,
                                                                  onTimeRangeChange,
                                                                  selectedAnalyzerId
                                                              }) => {
    const chartConfig = useMemo(() => ({
        temperature: {
            label: "Temperature",
            color: "hsl(var(--chart-temperature))"
        },
        humidity: {
            label: "Humidity",
            color: "hsl(var(--chart-humidity))"
        },
        rise: {
            label: "Growth",
            color: "hsl(var(--chart-growth))"
        }
    }), []);

    const chartContent = useMemo(() => {
        if (loading) {
            return (
                <div className="h-96 flex items-center justify-center">
                    <div className="text-center">
                        <RefreshCw className="h-8 w-8 animate-spin mx-auto mb-2 text-accent-foreground" />
                        <div className="text-accent-foreground">Loading chart data...</div>
                    </div>
                </div>
            );
        }

        // If no readings, show empty state
        if (readings.length === 0) {
            return (
                <div className="h-96 flex items-center justify-center">
                    <div className="text-center text-muted-foreground">
                        <TrendingUp className="h-8 w-8 mx-auto mb-2 opacity-30" />
                        <div className="text-sm opacity-60">
                            {selectedAnalyzerId ? 'No data for selected time range' : 'No analyzer selected'}
                        </div>
                    </div>
                </div>
            );
        }

        return (
            <ChartContainer
                key={`${selectedAnalyzerId}-${timeRange}-${readings.length}`}
                config={chartConfig}
                className="h-96 w-full"
            >
                <AreaChart
                    data={readings}
                    margin={{ top: 20, right: 10, left: 10, bottom: 5 }}
                >
                    <defs>
                        <linearGradient id="fillTemperature" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="5%" stopColor="hsl(var(--chart-temperature))" stopOpacity={0.8} />
                            <stop offset="95%" stopColor="hsl(var(--chart-temperature))" stopOpacity={0.1} />
                        </linearGradient>
                        <linearGradient id="fillHumidity" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="5%" stopColor="hsl(var(--chart-humidity))" stopOpacity={0.8} />
                            <stop offset="95%" stopColor="hsl(var(--chart-humidity))" stopOpacity={0.1} />
                        </linearGradient>
                        <linearGradient id="fillGrowth" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="5%" stopColor="hsl(var(--chart-growth))" stopOpacity={0.8} />
                            <stop offset="95%" stopColor="hsl(var(--chart-growth))" stopOpacity={0.1} />
                        </linearGradient>
                    </defs>
                    <CartesianGrid vertical={false} stroke="hsl(var(--border))" />
                    <XAxis
                        dataKey="date"
                        tickLine={false}
                        axisLine={false}
                        stroke="hsl(var(--foreground))"
                        interval="preserveStartEnd"
                        tickFormatter={(value) => {
                            const date = new Date(value);
                            return date.toLocaleTimeString("dk-DK", {
                                hour: "2-digit",
                                minute: "2-digit"
                            });
                        }}
                    />
                    <YAxis 
                        yAxisId="percentage"
                        tickLine={false}
                        axisLine={false}
                        stroke="hsl(var(--foreground))"
                        domain={['dataMin - 50', 'dataMax + 50']}
                        tickFormatter={(value) => `${value.toFixed(0)}%`}
                    />
                    <YAxis 
                        yAxisId="temperature"
                        orientation="right"
                        tickLine={false}
                        axisLine={false}
                        stroke="hsl(var(--foreground))"
                        domain={[0, 50]}
                        tickFormatter={(value) => `${value}°C`}
                        hide
                    />
                    <ChartTooltip
                        content={({ active, payload, label }) => {
                            if (!active || !payload?.length) return null;

                            return (
                                <div className="bg-card text-card-foreground border border-border p-3 shadow-lg rounded-lg">
                                    <p className="font-medium mb-2 text-sm text-accent-foreground">
                                        {new Date(label).toLocaleString("dk-DK", {
                                            day: "numeric",
                                            month: "numeric", 
                                            hour: "2-digit",
                                            minute: "2-digit"
                                        })}
                                    </p>
                                    <div className="space-y-1">
                                        {payload.map((entry, index) => {
                                            const displayName = entry.dataKey === 'rise' ? 'Growth' :
                                                entry.dataKey === 'temperature' ? 'Temperature' : 'Humidity';

                                            return (
                                                <div key={index} className="flex items-center justify-between gap-4">
                                                    <div className="flex items-center gap-2">
                                                        <div
                                                            className="w-2 h-2 rounded-full"
                                                            style={{ backgroundColor: entry.color }}
                                                        />
                                                        <span className="text-sm">{displayName}</span>
                                                    </div>
                                                    <span className="font-medium text-sm">
                                                        {typeof entry.value === 'number' ? entry.value.toFixed(1) : entry.value}
                                                        {entry.dataKey === "temperature" ? "°C" : "%"}
                                                    </span>
                                                </div>
                                            );
                                        })}
                                    </div>
                                </div>
                            );
                        }}
                    />
                    <Area 
                        yAxisId="temperature"
                        type="monotone" 
                        dataKey="temperature" 
                        stroke="hsl(var(--chart-temperature))" 
                        fill="url(#fillTemperature)" 
                        strokeWidth={2}
                    />
                    <Area 
                        yAxisId="percentage"
                        type="monotone" 
                        dataKey="humidity" 
                        stroke="hsl(var(--chart-humidity))" 
                        fill="url(#fillHumidity)" 
                        strokeWidth={2}
                    />
                    <Area 
                        yAxisId="percentage"
                        type="monotone" 
                        dataKey="rise" 
                        stroke="hsl(var(--chart-growth))" 
                        fill="url(#fillGrowth)" 
                        strokeWidth={2}
                    />
                </AreaChart>
            </ChartContainer>
        );
    }, [loading, readings, selectedAnalyzerId, timeRange, chartConfig]);

    return (
        <Card className="border-border-brown bg-bg-white overflow-hidden">
            <CardHeader className="bg-accent-foreground py-4 flex items-center justify-between">
                <div>
                    <CardTitle className="text-primary">Sourdough Development</CardTitle>
                    <div className="text-primary/80 text-sm">
                        Temperature, humidity and growth over time
                    </div>
                </div>
                <Select onValueChange={onTimeRangeChange} defaultValue={timeRange} disabled={loading}>
                    <SelectTrigger className="w-32 text-primary">
                        <SelectValue placeholder="Select time range" />
                    </SelectTrigger>
                    <SelectContent className="bg-white dark:bg-white text-black">
                        <SelectItem value="1h">
                            1 hour
                        </SelectItem>
                        <SelectItem value="6h">
                            6 hours
                        </SelectItem>
                        <SelectItem value="12h">
                            12 hours
                        </SelectItem>
                        <SelectItem value="24h">
                            24 hours
                        </SelectItem>
                    </SelectContent>
                </Select>
            </CardHeader>
            <CardContent className="p-0">
                {chartContent}
            </CardContent>
        </Card>
    );
};