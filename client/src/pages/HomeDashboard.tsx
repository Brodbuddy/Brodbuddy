import React, {useState, useEffect} from 'react';
import {
    Card,
    CardContent,
    CardHeader,
    CardTitle
} from '@/components/ui/card';
import {Area, AreaChart, CartesianGrid, XAxis} from "recharts";
import {
    ChartContainer,
    ChartTooltip,
} from "@/components/ui/chart";
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from "@/components/ui/select";
import {Droplet, Thermometer, TrendingUp} from 'lucide-react';
import {sourdoughHistoricalData} from '@/data/sourdoughData';
import SourdoughManager from '@/components/analyzer/SourdoughManager';
import {useWebSocket} from '@/hooks/useWebsocket';
import {useAuth} from '@/hooks/useAuth';
import {Broadcasts, SourdoughReading} from '@/api/websocket-client';

const HomeDashboard: React.FC = () => {
    const [timeRange, setTimeRange] = useState("12h");
    const [latestReading, setLatestReading] = useState<SourdoughReading | null>(null);
    const [realTimeData, setRealTimeData] = useState<Array<{
        date: string;
        temperature: number;
        humidity: number;
        growth: number;
    }>>([]);
    const {client, connected} = useWebSocket();
    const {user} = useAuth();

    const getDataFreshnessColor = (reading: SourdoughReading | null): string => {
        if (!reading) return "text-gray-400";

        const now = Date.now();
        const readingTime = new Date(reading.timestamp).getTime();
        const ageMinutes = (now - readingTime) / (1000 * 60);

        if (ageMinutes < 5) return "text-green-500";
        if (ageMinutes < 30) return "text-yellow-500";
        return "text-red-500";
    };

    useEffect(() => {
        if (!client || !connected || !user?.userId) return;

        const subscribeToData = async () => {
            try {
                await client.send.sourdoughData({
                    userId: user.userId
                });
            } catch (err) {
                console.error('Failed to subscribe to sourdough data:', err);
            }
        };

        subscribeToData();

        const unsubscribe = client.on(Broadcasts.sourdoughReading, (payload: SourdoughReading) => {
            console.log('Received sourdough reading:', payload);
            setLatestReading(payload);

            const newDataPoint = {
                date: payload.localTime,
                temperature: payload.temperature,
                humidity: payload.humidity,
                growth: payload.rise
            };

            console.log('New data point:', newDataPoint);
            console.log('Is valid date?', !isNaN(new Date(payload.localTime).getTime()));

            setRealTimeData(prevData => {
                const updatedData = [...prevData, newDataPoint];
                console.log('Updated realTimeData length:', updatedData.length);
                return updatedData.slice(-100);
            });
        });

        return () => unsubscribe();
    }, [client, connected]);

    const getFilteredData = () => {
        if (realTimeData.length === 0) return [];

        // For now, just return all data to see if it shows in the graph
        return realTimeData;
    };

    const chartData = getFilteredData();
    console.log('Chart data after filtering:', chartData);


    const latestData = sourdoughHistoricalData[sourdoughHistoricalData.length - 1];

    return (
        <>
            <SourdoughManager/>

            <Card className="border-border-brown bg-bg-cream shadow-md mt-6">

                <CardContent className="p-4 ">

                    <div className="bg-bg-cream">
                        {/* Header */}
                        <div className="flex justify-between items-center mb-6">
                            <h1 className="text-3xl font-bold text-accent-foreground p-2 rounded-md">My Sourdough</h1>
                            {latestReading && (
                                <div className="text-right">
                                    <div className="text-sm text-accent-foreground/60">Last updated</div>
                                    <div className="text-sm font-medium text-accent-foreground">
                                        {new Date(latestReading.localTime).toLocaleString()}
                                    </div>
                                    <div className="text-xs text-accent-foreground/40">
                                        {new Date(latestReading.timestamp).toLocaleString("en-US", {
                                            timeZone: "UTC",
                                            timeZoneName: "short"
                                        })}
                                    </div>
                                </div>
                            )}
                        </div>

                        {/* Main Content - Top Row */}
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12">

                            {/* Temperature Card */}
                            <Card className="border-border-brown bg-bg-white overflow-hidden">
                                <CardHeader className="bg-accent-foreground py-4">
                                    <CardTitle className="text-primary flex items-center justify-between">
                                        <div className="flex items-center">
                                            <Thermometer className="mr-2 h-5 w-5"/>
                                            Temperature
                                        </div>
                                        {latestReading && (
                                            <div
                                                className={`w-2 h-2 rounded-full ${getDataFreshnessColor(latestReading).replace('text-', 'bg-')}`}></div>
                                        )}
                                    </CardTitle>
                                </CardHeader>
                                <CardContent className="p-6 text-center">
                                    <div
                                        className="text-6xl font-bold text-accent-foreground mb-2">
                                        {latestReading ? `${latestReading.temperature.toFixed(1)}°C` : `${latestData.temperature}°C`}
                                    </div>
                                    <div className="text-accent-foreground/80">Latest water temperature</div>
                                </CardContent>
                            </Card>

                            {/* Humidity Card */}
                            <Card className="border-border-brown bg-bg-white overflow-hidden">
                                <CardHeader className="bg-accent-foreground py-4">
                                    <CardTitle className="text-primary flex items-center justify-between">
                                        <div className="flex items-center">
                                            <Droplet className="mr-2 h-5 w-5"/>
                                            Humidity
                                        </div>
                                        {latestReading && (
                                            <div
                                                className={`w-2 h-2 rounded-full ${getDataFreshnessColor(latestReading).replace('text-', 'bg-')}`}></div>
                                        )}
                                    </CardTitle>
                                </CardHeader>
                                <CardContent className="p-6 text-center">
                                    <div
                                        className="text-6xl font-bold text-accent-foreground mb-2">
                                        {latestReading ? `${latestReading.humidity.toFixed(1)}%` : `${latestData.humidity}%`}
                                    </div>
                                    <div className="text-accent-foreground/80">Latest humidity</div>
                                </CardContent>
                            </Card>

                            {/* Growth Card */}
                            <Card className="border-border-brown bg-bg-white overflow-hidden">
                                <CardHeader className="bg-accent-foreground py-4">
                                    <CardTitle className="text-primary flex items-center justify-between">
                                        <div className="flex items-center">
                                            <TrendingUp className="mr-2 h-5 w-5"/>
                                            Growth
                                        </div>
                                        {latestReading && (
                                            <div
                                                className={`w-2 h-2 rounded-full ${getDataFreshnessColor(latestReading).replace('text-', 'bg-')}`}></div>
                                        )}
                                    </CardTitle>
                                </CardHeader>
                                <CardContent className="p-6 text-center">
                                    <div
                                        className="text-6xl font-bold text-accent-foreground mb-2">
                                        {latestReading ? `${latestReading.rise.toFixed(1)}%` : `${latestData.growth}%`}
                                    </div>
                                    <div className="text-accent-foreground/80">Latest growth</div>
                                </CardContent>
                            </Card>
                        </div>

                        {/* Area Charts Section */}
                        <div className="grid grid-cols-1 gap-6">

                            <Card className="border-border-brown bg-bg-white overflow-hidden">
                                <CardHeader className="bg-accent-foreground py-4 flex items-center justify-between">
                                    <div>
                                        <CardTitle className="text-primary">Sourdough Development</CardTitle>
                                        <div className="text-primary/80 text-sm">Temperature, humidity and growth over
                                            time
                                        </div>
                                    </div>
                                    <Select onValueChange={(value: any) => setTimeRange(value)}
                                            defaultValue={timeRange}>
                                        <SelectTrigger className="bg-secondary border-border-brown">
                                            <SelectValue placeholder="Select time range"/>
                                        </SelectTrigger>
                                        <SelectContent className="bg-accent-foreground border-border-brown">
                                            <SelectItem value="1h"
                                                        className="text-primary hover:bg-bg-cream hover:text-accent-foreground">1
                                                hour</SelectItem>
                                            <SelectItem value="6h"
                                                        className="text-primary hover:bg-bg-cream hover:text-accent-foreground">6
                                                hours</SelectItem>
                                            <SelectItem value="12h"
                                                        className="text-primary hover:bg-bg-cream hover:text-accent-foreground">12
                                                hours</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </CardHeader>
                                <CardContent className="p-0">
                                    <ChartContainer
                                        config={{
                                            temperature: {
                                                label: "Temperature",
                                                color: "hsl(var(--chart-temperature))"
                                            },
                                            humidity: {
                                                label: "Humidity",
                                                color: "hsl(var(--chart-humidity))"
                                            },
                                            growth: {
                                                label: "Growth",
                                                color: "hsl(var(--chart-growth))"
                                            }
                                        }}
                                        className="h-96 w-full"
                                    >
                                        {/* Chart */}
                                        <AreaChart
                                            data={chartData}
                                            margin={{top: 20, right: 10, left: 10, bottom: 5}}
                                        >
                                            <defs>
                                                <linearGradient id="fillTemperature" x1="0" y1="0" x2="0" y2="1">
                                                    <stop offset="5%" stopColor="hsl(var(--chart-temperature))"
                                                          stopOpacity={0.8}/>
                                                    <stop offset="95%" stopColor="hsl(var(--chart-temperature))"
                                                          stopOpacity={0.1}/>
                                                </linearGradient>
                                                <linearGradient id="fillHumidity" x1="0" y1="0" x2="0" y2="1">
                                                    <stop offset="5%" stopColor="hsl(var(--chart-humidity))"
                                                          stopOpacity={0.8}/>
                                                    <stop offset="95%" stopColor="hsl(var(--chart-humidity))"
                                                          stopOpacity={0.1}/>
                                                </linearGradient>
                                                <linearGradient id="fillGrowth" x1="0" y1="0" x2="0" y2="1">
                                                    <stop offset="5%" stopColor="hsl(var(--chart-growth))"
                                                          stopOpacity={0.8}/>
                                                    <stop offset="95%" stopColor="hsl(var(--chart-growth))"
                                                          stopOpacity={0.1}/>
                                                </linearGradient>
                                            </defs>
                                            <CartesianGrid vertical={false} stroke="hsl(var(--border))"/>
                                            <XAxis
                                                dataKey="date"
                                                tickLine={false}
                                                axisLine={false}
                                                stroke="hsl(var(--foreground))"
                                                tickFormatter={(value) => {
                                                    const date = new Date(value);
                                                    return date.toLocaleDateString("dk-DK", {
                                                        month: "short",
                                                        day: "numeric",
                                                    });
                                                }}
                                            />
                                            <ChartTooltip
                                                content={({active, payload, label}) => {
                                                    if (active && payload && payload.length) {
                                                        return (
                                                            <div
                                                                className="bg-card text-card-foreground border border-border p-3 shadow-md rounded">
                                                                <p className="font-medium mb-3 text-primary border-b border-border pb-1">
                                                                    {new Date(label).toLocaleDateString("dk-DK", {
                                                                        month: "short",
                                                                        day: "numeric",
                                                                    })}
                                                                </p>
                                                                <div className="space-y-2">
                                                                    {payload.map((entry, index) => (
                                                                        <div key={`item-${index}`}
                                                                             className="flex items-center justify-between gap-8">
                                                                            <span className="text-sm"
                                                                                  style={{color: entry.color}}>{entry.name}:
                                                                            </span>
                                                                            <span className="font-medium"
                                                                                  style={{color: entry.color}}>
                                                                                    {entry.value}{entry.dataKey === "temperature" ? "°C" : "%"}
                                                                            </span>
                                                                        </div>
                                                                    ))}
                                                                </div>
                                                            </div>
                                                        );
                                                    }
                                                    return null;
                                                }}
                                            />
                                            <Area
                                                type="monotone"
                                                dataKey="growth"
                                                stroke="hsl(var(--chart-growth))"
                                                fill="url(#fillGrowth)"
                                            />
                                            <Area
                                                type="monotone"
                                                dataKey="humidity"
                                                stroke="hsl(var(--chart-humidity))"
                                                fill="url(#fillHumidity)"
                                            />
                                            <Area
                                                type="monotone"
                                                dataKey="temperature"
                                                stroke="hsl(var(--chart-temperature))"
                                                fill="url(#fillTemperature)"
                                            />


                                        </AreaChart>
                                    </ChartContainer>
                                </CardContent>
                            </Card>
                        </div>
                    </div>
                </CardContent>
            </Card>


        </>
    );
};

export default HomeDashboard;