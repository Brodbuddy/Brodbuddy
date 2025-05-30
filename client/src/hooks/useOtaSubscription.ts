import { useEffect, useState } from 'react';
import { useWebSocket } from './useWebsocket';
import { Broadcasts, OtaProgressUpdate } from '@/api/websocket-client';
import { toast } from 'sonner';

export const useOtaSubscription = (_analyzerIds: string[], onComplete?: () => void) => {
    const [otaProgress, setOtaProgress] = useState<Record<string, number>>({});
    const [updatingAnalyzers, setUpdatingAnalyzers] = useState<Set<string>>(new Set());
    const [subscribedAnalyzers, setSubscribedAnalyzers] = useState<Set<string>>(new Set());
    const { client, connected } = useWebSocket();

    useEffect(() => {
        if (!client || !connected) return;

        const unsubscribe = client.on(Broadcasts.otaProgressUpdate, (progress: OtaProgressUpdate) => {
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
                
                if (subscribedAnalyzers.has(progress.analyzerId)) {
                    client.send.otaProgressUnsubscription({ analyzerId: progress.analyzerId }).catch(err => {
                        console.error(`Failed to unsubscribe from OTA progress for ${progress.analyzerId}:`, err);
                    });
                    setSubscribedAnalyzers(prev => {
                        const newSet = new Set(prev);
                        newSet.delete(progress.analyzerId);
                        return newSet;
                    });
                }
                
                toast.success('Firmware update completed successfully!');
                onComplete?.();
            } else if (progress.status === 'failed') {
                setUpdatingAnalyzers(prev => {
                    const newSet = new Set(prev);
                    newSet.delete(progress.analyzerId);
                    return newSet;
                });
                
                if (subscribedAnalyzers.has(progress.analyzerId)) {
                    client.send.otaProgressUnsubscription({ analyzerId: progress.analyzerId }).catch(err => {
                        console.error(`Failed to unsubscribe from OTA progress for ${progress.analyzerId}:`, err);
                    });
                    setSubscribedAnalyzers(prev => {
                        const newSet = new Set(prev);
                        newSet.delete(progress.analyzerId);
                        return newSet;
                    });
                }
                
                toast.error(`Firmware update failed: ${progress.message}`);
            }
        });

        return () => {
            unsubscribe();
            subscribedAnalyzers.forEach(analyzerId => {
                client.send.otaProgressUnsubscription({ analyzerId }).catch(err => {
                    console.error(`Failed to unsubscribe from OTA progress for ${analyzerId}:`, err);
                });
            });
        };
    }, [client, connected, onComplete, subscribedAnalyzers]);

    const startOtaUpdate = async (userId: string, analyzerId: string, firmwareVersionId: string) => {
        if (!client || !connected) {
            toast.error('Not connected to server');
            return;
        }

        setUpdatingAnalyzers(prev => new Set(prev).add(analyzerId));
        
        try {
            if (!subscribedAnalyzers.has(analyzerId)) {
                await client.send.otaProgressSubscription({ analyzerId });
                setSubscribedAnalyzers(prev => new Set(prev).add(analyzerId));
                
                await new Promise(resolve => setTimeout(resolve, 500));
            }
            
            await client.send.startOtaUpdate({
                userId,
                analyzerId,
                firmwareVersionId
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

    return {
        otaProgress,
        updatingAnalyzers,
        startOtaUpdate
    };
};