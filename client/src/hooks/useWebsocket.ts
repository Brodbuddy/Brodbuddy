import { useAtomValue } from 'jotai';
import { clientIdAtom } from './import';
import { useEffect, useState, useMemo } from 'react';
import { WebSocketClient, WebSocketError, ErrorCodes } from '../api/websocket-client';
import { refreshToken } from "./useHttp";
import { tokenStorage, TOKEN_KEY, jwtAtom } from '../atoms/auth';
import config from '../config';

export function useWebSocket() {
    const [connected, setConnected] = useState(false);
    const [error, setError] = useState<Event | Error | null>(null);
    const clientId = useAtomValue(clientIdAtom);
    const jwt = useAtomValue(jwtAtom);

    const wsClient = useMemo(() => new WebSocketClient(config.wsUrl), []);
    
    useEffect(() => {
        if (!clientId) {
            wsClient.close();
            setConnected(false);
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
    }, [wsClient, clientId, jwt]);

    const authedClient = useMemo(() => {
        if (!wsClient.send) return wsClient;

        const proxiedSend = new Proxy(wsClient.send, {
            get<K extends keyof typeof wsClient.send>(
                target: typeof wsClient.send, 
                prop: K
            ): typeof wsClient.send[K] {
                const original = target[prop];
                
                if (typeof original !== 'function') {
                    return original;
                }

                return new Proxy(original, {
                    async apply(originalMethod: any, thisArg: any, argArray: any[]) {
                        try {
                            return await originalMethod.apply(thisArg, argArray);
                        } catch (error) {
                            if ((error as WebSocketError)?.code === ErrorCodes.unauthorized) {
                                try {
                                    await refreshToken();
                                    return await originalMethod.apply(thisArg, argArray);
                                } catch (refreshError) {
                                    throw refreshError;
                                }
                            }
                            throw error;
                        }
                    }
                });
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
        error
    };
}