import { v4 as uuidv4 } from 'uuid';

export enum LoggingLevel {
    Verbose = "Verbose",
    Debug = "Debug",
    Information = "Information",
    Warning = "Warning",
    Error = "Error",
    Fatal = "Fatal",
}

// WebSocket Error Codes
export const ErrorCodes = {
    invalidMessage: "INVALID_MESSAGE",
    missingFields: "MISSING_FIELDS",
    operationError: "OPERATION_ERROR",
    connectionError: "CONNECTION_ERROR",
    validationError: "VALIDATION_ERROR",
    unknownMessage: "UNKNOWN_MESSAGE",
    internalError: "INTERNAL_ERROR",
    unauthorized: "UNAUTHORIZED",
    forbidden: "FORBIDDEN",
} as const;

// Request type constants
export const Requests = {
    ping: "Ping",
    sourdoughData: "SourdoughData",
} as const;

// Response type constants
export const Responses = {
    pong: "Pong",
    sourdoughDataSubscribed: "SourdoughDataSubscribed",
} as const;

// Broadcast type constants
export const Broadcasts = {
    sourdoughReading: "SourdoughReading",
} as const;

// Subscription methods
export const SubscriptionMethods = {
    sourdoughData: "SourdoughData",
} as const;

// Unsubscription methods
export const UnsubscriptionMethods = {
} as const;

// Base interfaces
export interface BaseMessage {
    requestId?: string;
}

export interface BaseBroadcast {
    // Broadcasts don't have requestId
}

// Message interfaces
export interface Ping extends BaseMessage {
    Timestamp: number;
}

export interface Pong extends BaseMessage {
    Timestamp: number;
    ServerTimestamp: number;
}
 
export interface SubscribeToSourdoughData extends BaseMessage {
    UserId: string;
}

export interface SourdoughDataSubscribed extends BaseMessage {
    UserId: string;
    ConnectionId: string;
}

export interface SourdoughReading extends BaseBroadcast {
    RisePercent: number;
    TemperatureCelsius: number;
    HumidityPercent: number;
    Timestamp: string;
}

// Request-response type mapping
export type RequestResponseMap = {
    [Requests.ping]: [Ping, Pong];
    [Requests.sourdoughData]: [SubscribeToSourdoughData, SourdoughDataSubscribed];
};



export interface WebSocketError {
    code: string;
    message: string;
}

interface StoredSubscription {
    method: string;
    payload: any;
}

const WS_SUBSCRIPTION_KEY = "ws_subscriptions";

export class WebSocketClient {
    private socket: WebSocket | null = null;
    private pingInterval: number | null = null;
    private pendingRequests = new Map<string, { resolve: Function; reject: Function; timeout: number; }>();
    private listeners = new Map<string, Set<(payload: any) => void>>();
    private reconnectAttempts = 0;
    private reconnecting = false;
    private url: string;
    private clientId: string | null = null; 
    private getToken: (() => string | null) | null = null; 

    public onOpen: (() => void) | null = null;
    public onClose: (() => void) | null = null;
    public onError: ((error: Event) => void) | null = null;

    constructor(baseUrl: string) {
        this.url = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl;
    }

    public setCredentials(clientId: string, tokenProvider?: () => string | null): void {
        if (!clientId) {
            throw new Error("Client ID cannot be empty.");
        }
        this.clientId = clientId;
        this.getToken = tokenProvider || null;
    }

    public connect(): Promise<void> {
        if (!this.clientId) {
            return Promise.reject(new Error("Client ID not set."));
        }
        
        return new Promise((resolve, reject) => {
            if (this.socket?.readyState === WebSocket.OPEN) {
                resolve();
                return;
            }

            try {
                const connectUrl = `${this.url}/?id=${encodeURIComponent(this.clientId!)}`;
                this.socket = new WebSocket(connectUrl);

                this.socket.onopen = async () => {
                    this.reconnectAttempts = 0;
                    this.reconnecting = false;
                    
                    this.startPing();

                    if (this.onOpen) this.onOpen();
                    resolve();

                    setTimeout(async () => {
                        try {
                            await this.replaySubscriptions();
                        } catch (error) {
                            console.error('Failed to replay subscriptions:', error);
                        }
                    }, 100);
                };

                this.socket.onclose = () => {
                    if (this.onClose) this.onClose();
                    this.reconnect();
                };

                this.socket.onerror = (error) => {
                    if (this.onError) this.onError(error);
                };

                this.socket.onmessage = (event) => {
                    try {
                        const message = JSON.parse(event.data);
                        this.handleMessage(message);
                    } catch (error) {
                        console.error('Failed to parse message:', error);
                    }
                };
            } catch (error) {
                console.error('WebSocket connection failed:', error);
                reject(error);
            }
        });
    }

    public close(): void {
        if (this.pingInterval) {
            clearInterval(this.pingInterval);
            this.pingInterval = null;
        }
        
        if (this.socket) {
            this.socket.close();
            this.socket = null;
        }
    }

    private reconnect(): void {
        if (this.reconnecting) return;
        this.reconnecting = true;

        const maxReconnectAttempts = 5;
        if (this.reconnectAttempts >= maxReconnectAttempts) {
            return;
        }

        const delay = Math.min(1000 * 2 ** this.reconnectAttempts, 30000);
        this.reconnectAttempts++;

        setTimeout(() => {
            this.reconnecting = false;
            this.connect().catch(() => {});
        }, delay);
    }

    private handleMessage(message: any): void {
        const { Type, Payload, RequestId, TopicKey } = message;

        if (RequestId && this.pendingRequests.has(RequestId)) {
            const { resolve, reject, timeout } = this.pendingRequests.get(RequestId)!;
            clearTimeout(timeout);
            this.pendingRequests.delete(RequestId);

            if (Type === 'Error') {
                const error: WebSocketError = {
                    code: Payload.Code,
                    message: Payload.Message
                };
                reject(error);
            } else {
                resolve({ payload: Payload, topicKey: TopicKey });
            }
            return;
        }

        if (Type && this.listeners.has(Type)) {
            const handlers = this.listeners.get(Type)!;
            handlers.forEach(handler => handler(Payload));
        }
    }

    private async sendRequest<T>(type: string, payload: any, timeoutMs: number = 5000): Promise<T> {
        if (!this.socket || this.socket.readyState !== WebSocket.OPEN) {
            if (!this.clientId) {
                return Promise.reject('Client ID not set. Cannot send request.');
            }
            
            try {
                await this.connect();
            } catch (error) {
                return Promise.reject('Failed to connect');
            }
        }

        if (!this.socket || this.socket.readyState !== WebSocket.OPEN) {
            return Promise.reject('WebSocket is not open');
        }

        return new Promise<T>((resolve, reject) => {
            const requestId = uuidv4();
            const timeout = window.setTimeout(() => {
                if (this.pendingRequests.has(requestId)) {
                    this.pendingRequests.delete(requestId);
                    reject(new Error('Request timed out'));
                }
            }, timeoutMs);

            this.pendingRequests.set(requestId, {
                resolve: (result: { payload: any; topicKey?: string }) => { 
                    if (Object.values(SubscriptionMethods).includes(type as any)) {  
                        this.saveSubscription(type, payload, result.topicKey);
                    }

                    if (Object.values(UnsubscriptionMethods).includes(type as any)) { 
                        this.removeSubscription(type, payload, result.topicKey);
                    }

                    resolve(result.payload);
                },
                reject,
                timeout
            });

            const token = this.getToken ? this.getToken() : null;
            const messageToSend: any = {
                Type: type,
                Payload: payload,
                RequestId: requestId
            };
            if (token) {
                messageToSend.Token = token;
            }
            
            try {
                this.socket!.send(JSON.stringify(messageToSend));
            } catch (error) {
                clearTimeout(timeout);
                this.pendingRequests.delete(requestId);
                reject(error);
            }
        });
    }

    public on<T>(type: string, handler: (payload: T) => void): () => void {
        if (!this.listeners.has(type)) {
            this.listeners.set(type, new Set());
        }

        this.listeners.get(type)!.add(handler as any);

        return () => {
            const handlers = this.listeners.get(type);
            if (handlers) {
                handlers.delete(handler as any);
                if (handlers.size === 0) {
                    this.listeners.delete(type);
                }
            }
        };
    }
    
    private startPing(): void {
        if (this.pingInterval) {
            clearInterval(this.pingInterval);
        }

        this.pingInterval = window.setInterval(() => {
            if (this.socket?.readyState === WebSocket.OPEN) {
                this.send.ping({
                    Timestamp: Date.now()
                }).catch(error => {
                    console.warn("Ping failed:", error);
                });
            }
        }, 30000); // 30 seconds
    }

    private saveSubscription(method: string, payload: any, topicKey?: string): void {
        const subscriptions = JSON.parse(sessionStorage.getItem(WS_SUBSCRIPTION_KEY) || '{}') as Record<string, StoredSubscription>;
        const key = topicKey || `${method}:${JSON.stringify(payload)}`;
        subscriptions[key] = { method, payload };
        sessionStorage.setItem(WS_SUBSCRIPTION_KEY, JSON.stringify(subscriptions));
    }

    private removeSubscription(method: string, payload: any, topicKey?: string): void {
        const subscriptions = JSON.parse(sessionStorage.getItem(WS_SUBSCRIPTION_KEY) || '{}') as Record<string, StoredSubscription>;
        const key = topicKey || `${method}:${JSON.stringify(payload)}`;
        delete subscriptions[key];
        sessionStorage.setItem(WS_SUBSCRIPTION_KEY, JSON.stringify(subscriptions));
    }

    private async replaySubscriptions(): Promise<void> {
        const subscriptions = JSON.parse(sessionStorage.getItem(WS_SUBSCRIPTION_KEY) || '{}') as Record<string, StoredSubscription>;

        for (const { method, payload } of Object.values(subscriptions)) {
            try {
                await this.sendRequest(method, payload);
            } catch (error) {
                console.error(`Failed to replay ${method}:`, error);
            }
        }
    }


    send = {
    ping: (payload: Omit<Ping, 'requestId'>): Promise<Pong> => {
        return this.sendRequest<Pong>('Ping', payload);
    },
    sourdoughData: (payload: Omit<SubscribeToSourdoughData, 'requestId'>): Promise<SourdoughDataSubscribed> => {
        return this.sendRequest<SourdoughDataSubscribed>('SourdoughData', payload);
    },
};

}