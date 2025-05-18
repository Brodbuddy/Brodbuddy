import React, {useState} from 'react';
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
import {ThemeToggle} from "@/components/ThemeToggle";
import {sourdoughHistoricalData} from '@/data/sourdoughData';

const HomeDashboard: React.FC = () => {
    const [timeRange, setTimeRange] = useState("30d");


    const filteredData = sourdoughHistoricalData.filter(item => {
        const date = new Date(item.date);
        const today = new Date("2024-06-30");
        let daysToSubtract = 30;

        if (timeRange === "14d") {
            daysToSubtract = 14;
        } else if (timeRange === "7d") {
            daysToSubtract = 7;
        }

        const startDate = new Date(today);
        startDate.setDate(startDate.getDate() - daysToSubtract);

        return date >= startDate;
    });


    const latestData = sourdoughHistoricalData[sourdoughHistoricalData.length - 1];

    return (
        <>
            <div className="flex justify-end mr-4 mt-2">
                <ThemeToggle className="scale-150"/>
            </div>
            <Card className="border-border-brown bg-bg-cream shadow-md mt-10 ">

                <CardContent className="p-4 ">

                    <div className="bg-bg-cream">
                        {/* Header */}
                        <div className="flex justify-between items-center mb-6">
                            <h1 className="text-3xl font-bold text-accent-foreground p-2 rounded-md">My Sourdough</h1>
                        </div>

                        {/* Main Content - Top Row */}
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-12">

                            {/* Temperature Card */}
                            <Card className="border-border-brown bg-bg-white overflow-hidden">
                                <CardHeader className="bg-accent-foreground py-4">
                                    <CardTitle className="text-primary flex items-center">
                                        <Thermometer className="mr-2 h-5 w-5"/>
                                        Temperature
                                    </CardTitle>
                                </CardHeader>
                                <CardContent className="p-6 text-center">
                                    <div
                                        className="text-6xl font-bold text-accent-foreground mb-2">{latestData.temperature}°C
                                    </div>
                                    <div className="text-accent-foreground/80">Latest water temperature</div>
                                </CardContent>
                            </Card>

                            {/* Humidity Card */}
                            <Card className="border-border-brown bg-bg-white overflow-hidden">
                                <CardHeader className="bg-accent-foreground py-4">
                                    <CardTitle className="text-primary flex items-center">
                                        <Droplet className="mr-2 h-5 w-5"/>
                                        Humidity
                                    </CardTitle>
                                </CardHeader>
                                <CardContent className="p-6 text-center">
                                    <div
                                        className="text-6xl font-bold text-accent-foreground mb-2">{latestData.humidity}%
                                    </div>
                                    <div className="text-accent-foreground/80">Latest humidity</div>
                                </CardContent>
                            </Card>

                            {/* Growth Card */}
                            <Card className="border-border-brown bg-bg-white overflow-hidden">
                                <CardHeader className="bg-accent-foreground py-4">
                                    <CardTitle className="text-primary flex items-center">
                                        <TrendingUp className="mr-2 h-5 w-5"/>
                                        Growth
                                    </CardTitle>
                                </CardHeader>
                                <CardContent className="p-6 text-center">
                                    <div
                                        className="text-6xl font-bold text-accent-foreground mb-2">{latestData.growth}%
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
                                    <Select onValueChange={(value: any) => setTimeRange(value)} defaultValue={timeRange}>
                                        <SelectTrigger className="bg-secondary border-border-brown">
                                            <SelectValue placeholder="Select time range"/>
                                        </SelectTrigger>
                                        <SelectContent className="bg-accent-foreground border-border-brown">
                                            <SelectItem value="7d" className="text-primary hover:bg-bg-cream hover:text-accent-foreground">7 days</SelectItem>
                                            <SelectItem value="14d" className="text-primary hover:bg-bg-cream hover:text-accent-foreground">14 days</SelectItem>
                                            <SelectItem value="30d" className="text-primary hover:bg-bg-cream hover:text-accent-foreground">30 days</SelectItem>
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
                                            data={filteredData}
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