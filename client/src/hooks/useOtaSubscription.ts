import { useEffect, useState } from 'react';
import { useWebSocket } from './useWebsocket';
import { Broadcasts, OtaProgressUpdate } from '@/api/websocket-client';
import { toast } from 'sonner';

export const useOtaSubscription = (analyzerIds: string[], onComplete?: () => void) => {
    const [otaProgress, setOtaProgress] = useState<Record<string, number>>({});
    const [updatingAnalyzers, setUpdatingAnalyzers] = useState<Set<string>>(new Set());
    const { client, connected } = useWebSocket();

    useEffect(() => {
        if (!client || !connected || analyzerIds.length === 0) return;

        analyzerIds.forEach(analyzerId => {
            client.send.otaProgressSubscription({ analyzerId }).catch(err => {
                console.error(`Failed to subscribe to OTA progress for ${analyzerId}:`, err);
            });
        });

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
                toast.success('Firmware update completed successfully!');
                onComplete?.();
            } else if (progress.status === 'failed') {
                setUpdatingAnalyzers(prev => {
                    const newSet = new Set(prev);
                    newSet.delete(progress.analyzerId);
                    return newSet;
                });
                toast.error(`Firmware update failed: ${progress.message}`);
            }
        });

        return unsubscribe;
    }, [client, connected, analyzerIds, onComplete]);

    const startOtaUpdate = async (userId: string, analyzerId: string, firmwareVersionId: string) => {
        if (!client || !connected) {
            toast.error('Not connected to server');
            return;
        }

        setUpdatingAnalyzers(prev => new Set(prev).add(analyzerId));
        
        try {
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