import { useState, useEffect } from "react";
import { format } from "date-fns";
import { useAtom } from "jotai";
import { toast } from "sonner";
import { PlusCircle, Download, Loader2 } from "lucide-react";
import { api, analyzersAtom, useAuth } from "../import";
import { Button } from "@/components/ui/button";
import { useWebSocket } from "../../hooks/useWebsocket";
import { useFirmwareVersions } from "../../hooks";
import { Broadcasts } from "../../api/websocket-client";
import ActivationForm from "./ActivationForm";

interface AnalyzerListProps {
    onActivateClick: () => void;
}

function AnalyzerList({onActivateClick}: AnalyzerListProps) {
    const [analyzers, setAnalyzers] = useAtom(analyzersAtom);
    const [isLoading, setIsLoading] = useState(false);
    const [updatingAnalyzers, setUpdatingAnalyzers] = useState<Set<string>>(new Set());
    const [otaProgress, setOtaProgress] = useState<Record<string, number>>({});
    const { client } = useWebSocket();
    const { firmwareVersions } = useFirmwareVersions();
    const { user } = useAuth();

    useEffect(() => {
        loadAnalyzers();
    }, []);

    useEffect(() => {
        if (!client) return;

        const unsubscribe = client.on(Broadcasts.otaProgressUpdate, (progress: any) => {
            setOtaProgress(prev => ({
                ...prev,
                [progress.analyzerId]: progress.progress
            }));

            if (progress.status === 'complete') {
                setUpdatingAnalyzers(prev => {
                    const newSet = new Set(prev);
                    newSet.delete(progress.analyzerId);
                    return newSet;
                });
                setOtaProgress(prev => {
                    const newProgress = { ...prev };
                    delete newProgress[progress.analyzerId];
                    return newProgress;
                });
                toast.success('Firmware update completed successfully!');
                loadAnalyzers();
            } else if (progress.status === 'error') {
                setUpdatingAnalyzers(prev => {
                    const newSet = new Set(prev);
                    newSet.delete(progress.analyzerId);
                    return newSet;
                });
                setOtaProgress(prev => {
                    const newProgress = { ...prev };
                    delete newProgress[progress.analyzerId];
                    return newProgress;
                });
                toast.error('Firmware update failed');
            }
        });

        return unsubscribe;
    }, [client]);

    const loadAnalyzers = async () => {
        setIsLoading(true);
        try {
            const response = await api.analyzer.getUserAnalyzers();
            setAnalyzers(response.data);
        } catch (error: any) {
            toast.error("Failed to load analyzers", {
                description: error.response?.data?.message || "Please try again later"
            });
        } finally {
            setIsLoading(false);
        }
    };

    const handleUpdate = async (analyzerId: string) => {
        if (!client) {
            toast.error('WebSocket not connected');
            return;
        }

        const latestStableFirmware = firmwareVersions.find(f => f.isStable);
        if (!latestStableFirmware) {
            toast.error('No stable firmware available');
            return;
        }

        try {
            setUpdatingAnalyzers(prev => new Set(prev).add(analyzerId));
            
            await client.send.otaProgressSubscription({ analyzerId });
            
            await client.send.startOtaUpdate({
                userId: user?.userId || '',
                analyzerId,
                firmwareVersionId: latestStableFirmware.id
            });

            toast.info('Firmware update started');
        } catch (error) {
            setUpdatingAnalyzers(prev => {
                const newSet = new Set(prev);
                newSet.delete(analyzerId);
                return newSet;
            });
            toast.error('Failed to start firmware update');
        }
    };

    if (isLoading) {
        return (
            <div className="space-y-4">
                <div className="flex items-center justify-between">
                    <h3 className="text-lg font-medium">Your Devices</h3>
                </div>
                <div className="space-y-3">
                    {[1, 2].map((n) => (
                        <div key={n} className="h-20 rounded-lg border p-4 animate-pulse bg-gray-100 dark:bg-gray-800"/>
                    ))}
                </div>
            </div>
        );
    }

    if (!analyzers?.length) {
        return (
            <div className="mt-4 rounded-lg border p-6">
                <div className="mb-6">
                    <h2 className="text-xl font-semibold mb-1">Activate Your Device</h2>
                    <p className="text-muted-foreground">
                        Enter your 12-character activation code and choose a nickname for your device.
                    </p>
                </div>
                
                <div className="max-w-md mx-auto">
                    <ActivationForm onSuccess={() => {}} />
                </div>
                
                <div className="mt-4 pt-4 border-t text-sm text-muted-foreground">
                    Having trouble? Check the activation code on the bottom of your device or contact support.
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-4 mt-8">
            <div className="flex items-center justify-between">
                <h3 className="text-lg font-medium">Your Devices</h3>
                <Button onClick={onActivateClick} className="bg-orange-500 text-black hover:bg-orange-600 dark:bg-orange-600 dark:text-white text-sm px-3 py-1 h-8">
                    <PlusCircle className="mr-2 h-4 w-4"/>
                    Add Device
                </Button>
            </div>
            <div className="space-y-3">
                {analyzers.map((analyzer) => (
                    <div key={analyzer.id} className="flex items-center justify-between rounded-lg border p-4 hover:border-accent/50 transition-colors">
                        <div className="flex-1">
                            <div className="flex items-center gap-2">
                                <p className="font-medium">
                                    {analyzer.nickname || analyzer.name}{" "}
                                    {analyzer.isOwner && (
                                        <span className="text-xs bg-accent/20 text-accent-foreground px-2 py-0.5 rounded-full ml-2">Owner</span>
                                    )}
                                </p>
                                {analyzer.hasUpdate && (
                                    <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium bg-blue-100 text-blue-800 rounded-full">
                                        <Download className="h-3 w-3" />
                                        {(() => {
                                            const latest = firmwareVersions.find(f => f.isStable);
                                            return latest ? `Update to ${latest.version}` : 'Update Available';
                                        })()}
                                    </span>
                                )}
                            </div>
                            <div className="flex items-center gap-4 text-sm text-muted-foreground">
                                {analyzer.firmwareVersion && (
                                    <span>Firmware: {analyzer.firmwareVersion}</span>
                                )}
                                {analyzer.lastSeen && (
                                    <span>Last active: {format(new Date(analyzer.lastSeen), "MMM d, yyyy")}</span>
                                )}
                            </div>
                        </div>
                        <div className="flex items-center gap-2">
                            {analyzer.hasUpdate && (
                                <Button 
                                    size="sm" 
                                    disabled={updatingAnalyzers.has(analyzer.id)}
                                    onClick={() => handleUpdate(analyzer.id)}
                                    className="bg-accent-foreground text-primary-foreground hover:bg-accent-foreground/90"
                                >
                                    {updatingAnalyzers.has(analyzer.id) ? (
                                        <>
                                            <Loader2 className="h-4 w-4 mr-1 animate-spin" />
                                            {otaProgress[analyzer.id] ? `${otaProgress[analyzer.id]}%` : 'Starting...'}
                                        </>
                                    ) : (
                                        <>
                                            <Download className="h-4 w-4 mr-1" />
                                            Update
                                        </>
                                    )}
                                </Button>
                            )}
                            <Button variant="outline" size="sm" className="border-border hover:bg-accent hover:text-accent-foreground">
                                View
                            </Button>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}

export default AnalyzerList;