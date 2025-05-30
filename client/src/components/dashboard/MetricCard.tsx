import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { LucideIcon } from 'lucide-react';

interface MetricCardProps {
    title: string;
    value: number | null;
    unit: string;
    icon: LucideIcon;
    isLoading: boolean;
    showEmptyState?: boolean;
}

export const MetricCard: React.FC<MetricCardProps> = ({
                                                          title,
                                                          value,
                                                          unit,
                                                          icon: Icon,
                                                          isLoading,
                                                          showEmptyState = true,

                                                      }) => {


    if (!isLoading && value === null && !showEmptyState) {
        return null;
    }

    return (
        <Card className="border-border-brown bg-bg-white overflow-hidden">
            <CardHeader className="bg-accent-foreground py-4">
                <CardTitle className="text-primary flex items-center">
                    <Icon className="mr-2 h-5 w-5" />
                    {title}
                </CardTitle>
            </CardHeader>
            <CardContent className="p-6 text-center">
                {isLoading ? (
                    <Skeleton className="h-16 w-24 mx-auto mb-2" />
                ) : (
                    <>
                        <div className="text-6xl font-bold text-accent-foreground mb-2">
                            {value !== null ? `${value.toFixed(1)}${unit}` : '--'}
                        </div>
                        <div className="text-accent-foreground/80">
                            {value !== null ? `Latest ${title.toLowerCase()}` : 'No data'}
                        </div>
                    </>
                )}
            </CardContent>
        </Card>
    );
};