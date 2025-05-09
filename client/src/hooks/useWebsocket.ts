import { useAtomValue } from 'jotai';
import { clientIdAtom } from './import';
import { useEffect, useState, useMemo, useCallback } from 'react';
import { WebSocketClient, WebSocketError } from '../api/websocket-client';
import { refreshToken } from "./useHttp";
import { tokenStorage, TOKEN_KEY, jwtAtom } from '../atoms/auth';

const wsClient = new WebSocketClient('ws://localhost:8181');

interface WebSocketSessionState {
    roomId?: string;
    username?: string;
    [key: string]: any;
}

export function useWebSocket() {
    const [connected, setConnected] = useState(false);
    const [error, setError] = useState<Event | Error | null>(null);
    const [sessionRestored, setSessionRestored] = useState(false);
    const clientId = useAtomValue(clientIdAtom);
    const jwt = useAtomValue(jwtAtom)

    const saveSessionState = useCallback((state: WebSocketSessionState) => {
        localStorage.setItem('ws_session_state', JSON.stringify(state));
    }, []);

    const clearSessionState = useCallback(() => {
        localStorage.removeItem('ws_session_state');
    }, []);

    const getSavedSessionState = useCallback((): WebSocketSessionState | null => {
        const stateJson = localStorage.getItem('ws_session_state');
        if (!stateJson) return null;

        try {
            return JSON.parse(stateJson);
        } catch (error) {
            console.error('Error parsing session state:', error);
            localStorage.removeItem('ws_session_state');
            return null;
        }
    }, []);

    const handleReconnection = useCallback(async () => {
        if (!connected || sessionRestored) return;

        const savedState = getSavedSessionState();
        if (!savedState || !savedState.roomId) return;

        try {
            await authedClient.send.joinRoom({
                RoomId: savedState.roomId,
                Username: savedState.username || 'Anonymous'
            });

            setSessionRestored(true);
        } catch (error) {
            console.error('Failed to restore session:', error);
            clearSessionState();
        }
    }, [connected, sessionRestored]);
    
    useEffect(() => {
        if (!clientId) {
            wsClient.close();
            setConnected(false);
            setSessionRestored(false);
            return;
        }

        const tokenProvider = () => tokenStorage.getItem(TOKEN_KEY, null);
        wsClient.setCredentials(clientId, tokenProvider)
        
        wsClient.onOpen = () => {
            setConnected(true);
            setError(null);
        }
        
        wsClient.onClose = () => {
            setConnected(false);
            setSessionRestored(false);
        }
        
        wsClient.onError = (err: Event | Error) => { 
            setError(err);
            setConnected(false);
        };
        
        wsClient.connect().catch()

        return () => {
            wsClient.onOpen = null;
            wsClient.onClose = null;
            wsClient.onError = null;
        };
    }, [clientId, jwt]);

    useEffect(() => {
        if (connected && !sessionRestored) {
            handleReconnection();
        }
    }, [connected, sessionRestored, handleReconnection]);

    const authedClient = useMemo(() => {
        if (!wsClient.send) return wsClient;

        const proxiedSend = new Proxy(wsClient.send, {
            get(target, prop) {
                const originalMethod = target[prop as keyof typeof target];

                if (typeof originalMethod !== 'function') return originalMethod;

                return async function(payload: any) {
                    try {
                        return await originalMethod.call(target, payload);
                    } catch (error) {
                        if ((error as WebSocketError)?.code === 'UNAUTHORIZED') {
                            try {
                                await refreshToken();
                                return await originalMethod.call(target, payload);
                            } catch (refreshError) {
                                throw refreshError;
                            }
                        }
                        throw error;
                    }
                };
            }
        });

        return {
            ...wsClient,
            send: proxiedSend,
            on: wsClient.on.bind(wsClient), 
            connect: wsClient.connect.bind(wsClient),
            close: wsClient.close.bind(wsClient),
            setCredentials: wsClient.setCredentials.bind(wsClient)
        };
    }, [wsClient]);


    return {
        client: authedClient,
        connected,
        error,
        saveSessionState,
        sessionRestored
    };
}