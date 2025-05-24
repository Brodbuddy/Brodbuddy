import {useState, useEffect} from 'react';
import {useWebSocket} from '../hooks/useWebsocket.ts';
import {Broadcasts} from "../api/websocket-client.ts";

export default function Home() {
    const {client, connected} = useWebSocket();
    const [isJoined, setIsJoined] = useState(false);
    const [joinedUsers, setJoinedUsers] = useState<string[]>([]);

    useEffect(() => {
        if (!client) return;
        
        const unsubscribe = client.on(Broadcasts.sourdoughReading, (payload: any) => {
            setJoinedUsers(prev => [...prev, `Temperature reading: ${payload.TemperatureCelsius}`]);
        });
        
        return () => unsubscribe();
    }, [client]);

    const handleJoinRoom = () => {
        if (!client || !connected) return;
        client.send.sourdoughData({
            UserId: "38915d56-2322-4a6b-8506-a1831535e62b"
        }).then(response => {
            setIsJoined(true);
            setJoinedUsers(prev => [...prev, `Subscribed to telemetry for user ${response.UserId} with connection ${response.ConnectionId}`]);
            console.log("UserID: " + response.UserId);
        }).catch(error => {
            console.error('Failed to subscribe to telemetry:', error);
        });
    };
    
    useEffect(() => {
        if (!client || !connected) return;

        const subscriptions = JSON.parse(sessionStorage.getItem('ws_subscriptions') || '{}');
        const isAlreadyJoined = Object.values(subscriptions).some((sub: any) =>
            sub.method === 'Telemetry' && sub.payload?.UserId === "38915d56-2322-4a6b-8506-a1831535e62b"
        );

        if (isAlreadyJoined) {
            setIsJoined(true);
            setJoinedUsers(prev => [
                ...prev,
                `Reconnected to telemetry stream`
            ]);
        }
    }, [client, connected]);

    return (
        <div
            style={{position: 'absolute', left: '50%', top: '50%', transform: 'translate(-50%, -50%)', width: '500px'}}>
            <h1 style={{textAlign: 'center'}}>Sourdough Telemetry</h1>

            {!isJoined ? (
                <div style={{border: '1px solid #ccc', padding: '20px', borderRadius: '5px'}}>
                    <h2>Subscribe to Telemetry</h2>
                    <button
                        onClick={handleJoinRoom}
                        style={{
                            padding: '10px 16px',
                            backgroundColor: connected ? '#3b82f6' : '#ccc',
                            color: 'white',
                            border: 'none',
                            cursor: connected ? 'pointer' : 'not-allowed',
                            width: '100%'
                        }}
                        disabled={!connected}
                    >
                        Subscribe to Telemetry
                    </button>

                    {!connected && (
                        <div style={{marginTop: '10px', color: 'red', textAlign: 'center'}}>
                            Not connected to server
                        </div>
                    )}
                </div>
            ) : (
                <div>
                    <div style={{
                        marginBottom: '10px',
                        padding: '10px',
                        backgroundColor: '#f0f0f0',
                        borderRadius: '5px'
                    }}>
                        Subscribed to telemetry stream
                    </div>

                    <div style={{
                        height: '400px',
                        overflowY: 'auto',
                        border: '1px solid #ccc',
                        marginBottom: '10px',
                        padding: '10px',
                        backgroundColor: 'white'
                    }}>
                        {joinedUsers.map((user, idx) => (
                            <div key={idx} style={{
                                marginBottom: '10px',
                                padding: '10px',
                                backgroundColor: '#f0f0f0',
                                borderRadius: '5px'
                            }}>
                                {user}
                            </div>
                        ))}
                        {joinedUsers.length === 0 && (
                            <div style={{textAlign: 'center', color: '#666'}}>
                                No telemetry data yet. Send MQTT messages to see data appear here.
                            </div>
                        )}
                    </div>

                    <div style={{textAlign: 'center', color: '#666', fontStyle: 'italic'}}>
                        Waiting for MQTT telemetry data...
                    </div>
                </div>
            )}
        </div>
    );
};