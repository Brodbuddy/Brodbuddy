import { useState, useEffect } from 'react';
import { useWebSocket } from '../hooks/useWebsocket';
import { TelemetryReading } from '../api/Api.ts';

export default function TelemetryPage() {
    const { client, connected } = useWebSocket();
    const [telemetryData, setTelemetryData] = useState<TelemetryReading[]>([]);
    const [isSubscribed, setIsSubscribed] = useState(false);
    const [deviceId, setDeviceId] = useState('sourdough_monitor_01');

    useEffect(() => {
        if (!client || !connected) return;

        const handleMessage = (event: MessageEvent) => {
            try {
                const data = JSON.parse(event.data);
                const payload = data.Payload;

                if (payload?.DeviceId && payload?.Distance !== undefined && payload?.RisePercentage !== undefined) {
                    const reading: TelemetryReading = {
                        deviceId: payload.DeviceId,
                        distance: payload.Distance,
                        risePercentage: payload.RisePercentage,
                        timestamp: payload.Timestamp || new Date().toISOString()
                    };

                    // Tilføj til telemetry hvis dataen ikke allerede eksistere
                    setTelemetryData(prev => {
                        if (prev.some(item => item.timestamp === reading.timestamp)) {
                            return prev;
                        }
                        return [reading, ...prev].slice(0, 20);
                    });
                }
            } catch (error) {
                console.error('Error processing WebSocket message:', error);
            }
        };

        client.socket.addEventListener('message', handleMessage);

        // Cleanup
        return () => {
            client.socket.removeEventListener('message', handleMessage);
        };
    }, [client, connected]);

    const subscribeToTelemetry = async () => {
        if (!client || !connected || isSubscribed) return;

        try {
            await client.send.joinRoom({
                RoomId: `telemetry/${deviceId}`,
                Username: 'TelemetryViewer'
            });

            setIsSubscribed(true);
        } catch (error) {
            console.error('Failed to subscribe to telemetry:', error);
        }
    };

    return (
        <div className="container mx-auto p-4">
            <h1 className="text-2xl font-bold mb-4">Sourdough Telemetry Monitor</h1>

            <div className="mb-6">
                <div className="flex gap-2 mb-4">
                    <input
                        type="text"
                        value={deviceId}
                        onChange={(e) => setDeviceId(e.target.value)}
                        placeholder="Device ID"
                        className="border p-2 rounded"
                        disabled={isSubscribed}
                    />
                    <button
                        onClick={subscribeToTelemetry}
                        disabled={!connected || isSubscribed}
                        className={`p-2 rounded ${
                            !connected ? 'bg-gray-300' :
                                isSubscribed ? 'bg-green-500 text-white' : 'bg-blue-500 text-white'
                        }`}
                    >
                        {isSubscribed ? 'Subscribed' : 'Subscribe'}
                    </button>
                </div>

                <div className="text-sm">
                    {!connected && <p className="text-red-500">Not connected to WebSocket server</p>}
                    {connected && !isSubscribed && <p>Click Subscribe to start receiving telemetry data</p>}
                    {connected && isSubscribed && <p className="text-green-500">Listening for telemetry data...</p>}
                </div>
            </div>

            <div className="border rounded p-4">
                <h2 className="text-xl mb-2">Latest Readings</h2>

                {telemetryData.length === 0 ? (
                    <p className="text-gray-500">No telemetry data received yet</p>
                ) : (
                    <table className="w-full border-collapse">
                        <thead>
                        <tr>
                            <th className="border p-2 text-left">Timestamp</th>
                            <th className="border p-2 text-left">Device ID</th>
                            <th className="border p-2 text-left">Distance (mm)</th>
                            <th className="border p-2 text-left">Rise (%)</th>
                        </tr>
                        </thead>
                        <tbody>
                        {telemetryData.map((reading, index) => (
                            <tr key={index}>
                                <td className="border p-2">{new Date(reading.timestamp).toLocaleString()}</td>
                                <td className="border p-2">{reading.deviceId || 'Unknown'}</td>
                                <td className="border p-2">{reading.distance?.toFixed(2) || 'N/A'}</td>
                                <td className="border p-2">{reading.risePercentage?.toFixed(2) || 'N/A'}</td>
                            </tr>
                        ))}
                        </tbody>
                    </table>
                )}
            </div>
        </div>
    );
}