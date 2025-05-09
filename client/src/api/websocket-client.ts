// Message type constants
export const MessageType = {
    joinRoom: "JoinRoom",
    userJoined: "UserJoined",
} as const;

// Base message interface
export interface BaseMessage {
    requestId?: string;
}

// Message interfaces
export interface JoinRoom extends BaseMessage {
    RoomId: string;
    Username: string;
}

export interface UserJoined extends BaseMessage {
    RoomId: string;
    Username: string;
    ConnectionId: string;
}

// Request-response type mapping
export type RequestResponseMap = {
    [MessageType.joinRoom]: [JoinRoom, UserJoined];
};




export interface WebSocketError {
    code: string;
    message: string;
}

export class WebSocketClient {
    private socket: WebSocket | null = null;
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

                this.socket.onopen = () => {
                    this.reconnectAttempts = 0;
                    this.reconnecting = false;
                    if (this.onOpen) this.onOpen();
                    resolve();
                };

                this.socket.onclose = () => {
                    if (this.onClose) this.onClose();
                    this.reconnect();
                    reject(new Error('Connection closed'));
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
        const { Type, Payload, RequestId } = message;

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
                resolve(Payload);
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
            const requestId = crypto.randomUUID();
            const timeout = window.setTimeout(() => {
                if (this.pendingRequests.has(requestId)) {
                    this.pendingRequests.delete(requestId);
                    reject(new Error('Request timed out'));
                }
            }, timeoutMs);

            this.pendingRequests.set(requestId, { resolve, reject, timeout });

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

    send = {
    joinRoom: (payload: Omit<JoinRoom, 'requestId'>): Promise<UserJoined> => {
        return this.sendRequest<UserJoined>('JoinRoom', payload);
    },
};

}