import { useState, useEffect } from 'react';
import { useWebSocket } from '../hooks/useWebsocket';
import { Broadcasts } from '../api/websocket-client';

export function WebSocketTest() {
    const { client, connected, error } = useWebSocket();
    const [telemetryResult, setTelemetryResult] = useState<string>('');
    const [pingResult, setPingResult] = useState<string>('');
    const [readings, setReadings] = useState<string[]>([]);

    useEffect(() => {
        if (!client) return;
        
        const unsubscribe = client.on(Broadcasts.sourdoughReading, (payload: any) => {
            console.log(payload);
            setReadings(prev => [...prev, `Temperature: ${payload.Temperature}`]);
        });
        
        return () => unsubscribe();
    }, [client]);

    const handleTelemetrySubscribe = async () => {
        try {
            const result = await client.send.sourdoughData({
                userId: '38915d56-2322-4a6b-8506-a1831535e62b'
            });
            setTelemetryResult(`Connected: ${result.connectionId} for User: ${result.userId}`);
        } catch (err) {
            setTelemetryResult(`Telemetry error: ${JSON.stringify(err)}`);
        }
    };

    const handlePing = async () => {
        try {
            const result = await client.send.ping({
                timestamp: Date.now()
            });
            const latency = Date.now() - result.timestamp;
            setPingResult(`Pong received! Latency: ${latency}ms`);
        } catch (err) {
            setPingResult(`Ping error: ${JSON.stringify(err)}`);
        }
    };

    return (
        <div style={{ padding: '20px' }}>
            <h2>WebSocket Test</h2>
            <div>
                <strong>Connection Status:</strong> {connected ? 'Connected' : 'Disconnected'}
            </div>
            {error && (
                <div style={{ color: 'red' }}>
                    <strong>Error:</strong> {error.toString()}
                </div>
            )}
            
            <div style={{ marginTop: '20px' }}>
                <button onClick={handleTelemetrySubscribe} disabled={!connected}>
                    Subscribe to Telemetry
                </button>
                <div>{telemetryResult}</div>
            </div>
            
            <div style={{ marginTop: '20px' }}>
                <button onClick={handlePing} disabled={!connected}>
                    Send Ping
                </button>
                <div>{pingResult}</div>
            </div>
            
            <div style={{ marginTop: '20px' }}>
                <h3>Sourdough Readings:</h3>
                <div style={{ 
                    height: '200px', 
                    overflowY: 'auto', 
                    border: '1px solid #ccc', 
                    padding: '10px',
                    backgroundColor: '#f9f9f9'
                }}>
                    {readings.length === 0 ? (
                        <div style={{ color: '#666' }}>No readings yet. Subscribe to telemetry first.</div>
                    ) : (
                        readings.map((reading, idx) => (
                            <div key={idx} style={{ 
                                marginBottom: '5px', 
                                padding: '5px',
                                backgroundColor: '#fff',
                                borderRadius: '3px'
                            }}>
                                {reading}
                            </div>
                        ))
                    )}
                </div>
            </div>
        </div>
    );
}