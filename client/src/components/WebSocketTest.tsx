import { useState } from 'react';
import { useWebSocket } from '../hooks/useWebsocket';

export function WebSocketTest() {
    const { client, connected, error } = useWebSocket();
    const [joinResult, setJoinResult] = useState<string>('');
    const [pingResult, setPingResult] = useState<string>('');

    const handleJoinRoom = async () => {
        try {
            const result = await client.send.joinRoom({
                RoomId: 'test-room',
                Username: 'TestUser'
            });
            setJoinResult(`Joined room: ${result.RoomId} as ${result.Username}`);
        } catch (err) {
            setJoinResult(`Join error: ${JSON.stringify(err)}`);
        }
    };

    const handlePing = async () => {
        try {
            const result = await client.send.ping({
                Timestamp: Date.now()
            });
            const latency = Date.now() - result.Timestamp;
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
                <button onClick={handleJoinRoom} disabled={!connected}>
                    Join Room
                </button>
                <div>{joinResult}</div>
            </div>
            
            <div style={{ marginTop: '20px' }}>
                <button onClick={handlePing} disabled={!connected}>
                    Send Ping
                </button>
                <div>{pingResult}</div>
            </div>
        </div>
    );
}