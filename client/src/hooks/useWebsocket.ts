import { useAtomValue } from 'jotai';
import { clientIdAtom } from './import';
import { useEffect, useState } from 'react';
import { WebSocketClient } from '../api/websocket-client';

const wsClient = new WebSocketClient('ws://localhost:8181');

export function useWebSocket() {
    const [connected, setConnected] = useState(false);
    const [error, setError] = useState<Event | Error | null>(null);

    const clientId = useAtomValue(clientIdAtom);
    
    useEffect(() => {
        if (!clientId) {
            wsClient.close();
            setConnected(false);
            return;
        }

        wsClient.setCredentials(clientId)
        
        wsClient.onOpen = () => {
            setConnected(true);
            setError(null);
        }
        
        wsClient.onClose = () => {
            setConnected(false);
        }
        
        wsClient.onError = (err: Event | Error) => { 
            setError(err); setConnected(false);
        };
        
        wsClient.connect().catch()

        return () => {
            wsClient.onOpen = null;
            wsClient.onClose = null;
            wsClient.onError = null;
        };
    }, [clientId]);

    return {
        client: wsClient,
        connected,
        error
    };
}