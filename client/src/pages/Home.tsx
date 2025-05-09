import { useState, useEffect } from 'react';
import { useWebSocket } from '../hooks/useWebsocket.ts';
import { MessageType } from "../api/websocket-client.ts";

export default function Home() {
    const { client, connected, saveSessionState, sessionRestored } = useWebSocket();
    const [username, setUsername] = useState('');
    const [roomId, setRoomId] = useState('default');
    const [isJoined, setIsJoined] = useState(false);
    const [joinedUsers, setJoinedUsers] = useState<string[]>([]);

    useEffect(() => {
        if (!client) return;
        const unsubscribe = client.on(MessageType.userJoined, (payload: any) => {
            setJoinedUsers(prev => [...prev, `User ${payload.ConnectionId} joined room ${payload.RoomId}`]);
        });

        return () => unsubscribe();
    }, [client]);

    useEffect(() => {
        if (sessionRestored) {
            const sessionStateJson = localStorage.getItem('ws_session_state');
            if (!sessionStateJson) return;

            try {
                const sessionState = JSON.parse(sessionStateJson);

                if (sessionState.username) setUsername(sessionState.username);
                if (sessionState.roomId) setRoomId(sessionState.roomId);
                setIsJoined(true);
                setJoinedUsers(prev =>
                    prev.length === 0
                        ? [`You rejoined room ${sessionState.roomId} as ${sessionState.username}`]
                        : prev
                );
            } catch (error) {
                console.error('Error parsing session state:', error);
            }
        }
    }, [sessionRestored]);

    const handleJoinRoom = () => {
        if (!client || !connected || !username.trim() || !roomId.trim()) return;
        client.send.joinRoom({
            RoomId: roomId,
            Username: username
        })
            .then(response => {
                saveSessionState({
                    roomId,
                    username,
                    connectionId: response.ConnectionId
                });

                setIsJoined(true);
                setJoinedUsers(prev => [...prev, `You joined room ${response.RoomId} with ID ${response.ConnectionId}`]);
            })
            .catch(error => {
                console.error('Failed to join room:', error);
            });
    };

    return (
        <div style={{ position: 'absolute', left: '50%', top: '50%', transform: 'translate(-50%, -50%)', width: '500px' }}>
            <h1 style={{ textAlign: 'center' }}>WebSocket Chat Room</h1>

            {!isJoined ? (
                <div style={{ border: '1px solid #ccc', padding: '20px', borderRadius: '5px' }}>
                    <h2>Join a Room</h2>
                    <div style={{ marginBottom: '10px' }}>
                        <label style={{ display: 'block', marginBottom: '5px' }}>Username:</label>
                        <input
                            type="text"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            style={{ width: '100%', padding: '8px', border: '1px solid #ccc' }}
                            disabled={!connected}
                        />
                    </div>
                    <div style={{ marginBottom: '20px' }}>
                        <label style={{ display: 'block', marginBottom: '5px' }}>Room ID:</label>
                        <input
                            type="text"
                            value={roomId}
                            onChange={(e) => setRoomId(e.target.value)}
                            style={{ width: '100%', padding: '8px', border: '1px solid #ccc' }}
                            disabled={!connected}
                        />
                    </div>
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
                        Join Room
                    </button>

                    {!connected && (
                        <div style={{ marginTop: '10px', color: 'red', textAlign: 'center' }}>
                            Not connected to server
                        </div>
                    )}
                </div>
            ) : (
                <div>
                    <div style={{ marginBottom: '10px', padding: '10px', backgroundColor: '#f0f0f0', borderRadius: '5px' }}>
                        Connected to room <strong>{roomId}</strong> as <strong>{username}</strong>
                    </div>

                    <div style={{ height: '400px', overflowY: 'auto', border: '1px solid #ccc', marginBottom: '10px', padding: '10px', backgroundColor: 'white' }}>
                        {joinedUsers.map((user, idx) => (
                            <div key={idx} style={{marginBottom: '10px', padding: '10px', backgroundColor: '#f0f0f0', borderRadius: '5px'}}>
                                {user}
                            </div>
                        ))}
                        {joinedUsers.length === 0 && (
                            <div style={{ textAlign: 'center', color: '#666' }}>
                                No users yet
                            </div>
                        )}
                    </div>

                    <div style={{ textAlign: 'center', color: '#666', fontStyle: 'italic' }}>
                        Note: You can only join rooms for now. Message sending is not implemented yet.
                    </div>
                </div>
            )}
        </div>
    );
};