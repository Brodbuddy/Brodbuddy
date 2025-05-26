import { useState, useEffect } from 'react';
import { useWebSocket } from '../hooks/useWebsocket';
import { Broadcasts } from '../api/websocket-client';
import type { OtaProgressUpdate, FirmwareAvailable } from '../api/websocket-client';

export default function OtaTestPage() {
  const { client, connected, error } = useWebSocket();
  const [subscribed, setSubscribed] = useState(false);
  const [otaProgress, setOtaProgress] = useState<OtaProgressUpdate | null>(null);
  const [otaHistory, setOtaHistory] = useState<OtaProgressUpdate[]>([]);
  const [firmwareNotifications, setFirmwareNotifications] = useState<FirmwareAvailable[]>([]);
  const [results, setResults] = useState<{ [key: string]: string }>({});
  
  // Test data
  const testAnalyzerId = 'fcb8e714-cd08-4049-bf6e-b1e1be96e6b4';
  const testUserId = '460b3d96-776f-4e4c-b2d8-943ceb1597a4';
  const testFirmwareId = '1c5543e7-6d96-417d-b320-570e47b3e66e';

  useEffect(() => {
    if (!client) return;
    
    // Listen for OTA progress updates
    const unsubscribeOtaProgress = client.on(Broadcasts.otaProgressUpdate, (data: OtaProgressUpdate) => {
      console.log('OTA Progress Update:', data);
      setOtaProgress(data);
      setOtaHistory(prev => [...prev, data]);
    });

    // Listen for firmware availability notifications
    const unsubscribeFirmware = client.on(Broadcasts.firmwareAvailable, (data: FirmwareAvailable) => {
      console.log('Firmware Available:', data);
      setFirmwareNotifications(prev => [...prev, data]);
    });

    return () => {
      unsubscribeOtaProgress();
      unsubscribeFirmware();
    };
  }, [client]);

  const subscribeToFirmwareNotifications = async () => {
    if (!client || !connected) return;
    
    try {
      const response = await client.send.firmwareNotificationSubscription({ clientType: 'web' });
      console.log('Subscribed to firmware notifications:', response);
      setResults(prev => ({ ...prev, firmwareSubscribe: `Subscribed to: ${response.topic || 'unknown topic'}` }));
    } catch (error) {
      console.error('Failed to subscribe to firmware notifications:', error);
      setResults(prev => ({ ...prev, firmwareSubscribe: `Error: ${JSON.stringify(error)}` }));
    }
  };

  const subscribeToOtaProgress = async () => {
    if (!client || !connected) return;
    
    try {
      const response = await client.send.otaProgressSubscription({
        analyzerId: testAnalyzerId
      });
      console.log('Subscribed to OTA progress:', response);
      setResults(prev => ({ ...prev, subscribe: `Subscribed to topic: ${response.topic || 'unknown topic'}` }));
      setSubscribed(true);
    } catch (error) {
      console.error('Failed to subscribe to OTA progress:', error);
      setResults(prev => ({ ...prev, subscribe: `Error: ${JSON.stringify(error)}` }));
    }
  };

  const startOtaUpdate = async () => {
    if (!client || !connected) return;
    
    try {
      const response = await client.send.startOtaUpdate({
        userId: testUserId,
        analyzerId: testAnalyzerId,
        firmwareVersionId: testFirmwareId
      });
      console.log('OTA update started:', response);
      setResults(prev => ({ ...prev, startOta: `Update started: ${response.updateId} (${response.status})` }));
    } catch (error) {
      console.error('Failed to start OTA update:', error);
      setResults(prev => ({ ...prev, startOta: `Error: ${JSON.stringify(error)}` }));
    }
  };

  const makeFirmwareAvailable = async () => {
    if (!client || !connected) return;
    
    try {
      const response = await client.send.makeFirmwareAvailable({
        firmwareId: testFirmwareId
      });
      console.log('Firmware made available:', response);
      setResults(prev => ({ ...prev, firmware: `Firmware available: ${response.firmwareId}` }));
    } catch (error) {
      console.error('Failed to make firmware available:', error);
      setResults(prev => ({ ...prev, firmware: `Error: ${JSON.stringify(error)}` }));
    }
  };

  const clearLogs = () => {
    setOtaProgress(null);
    setOtaHistory([]);
    setFirmwareNotifications([]);
    setResults({});
  };

  return (
    <div className="p-6 max-w-4xl mx-auto">
      <h1 className="text-2xl font-bold mb-6">OTA WebSocket Test Page</h1>
      
      {/* Connection Status */}
      <div className="mb-6 p-4 rounded-lg border">
        <h2 className="text-lg font-semibold mb-2">Connection Status</h2>
        <div className="flex items-center gap-2">
          <div className={`w-3 h-3 rounded-full ${connected ? 'bg-green-500' : 'bg-red-500'}`}></div>
          <span>{connected ? 'Connected' : 'Disconnected'}</span>
        </div>
        {error && (
          <div className="text-red-500 mt-2">
            <strong>Error:</strong> {error.toString()}
          </div>
        )}
        {subscribed && (
          <div className="flex items-center gap-2 mt-2">
            <div className="w-3 h-3 rounded-full bg-blue-500"></div>
            <span>Subscribed to OTA progress for {testAnalyzerId}</span>
          </div>
        )}
      </div>

      {/* Test Controls */}
      <div className="mb-6 p-4 rounded-lg border">
        <h2 className="text-lg font-semibold mb-4">Test Controls</h2>
        <div className="flex gap-2 flex-wrap">
          <button
            onClick={subscribeToFirmwareNotifications}
            disabled={!connected}
            className="px-4 py-2 bg-orange-500 text-white rounded disabled:bg-gray-300"
          >
            Subscribe to Firmware Notifications
          </button>
          
          <button
            onClick={subscribeToOtaProgress}
            disabled={!connected || subscribed}
            className="px-4 py-2 bg-blue-500 text-white rounded disabled:bg-gray-300"
          >
            Subscribe to OTA Progress
          </button>
          
          <button
            onClick={startOtaUpdate}
            disabled={!connected}
            className="px-4 py-2 bg-green-500 text-white rounded disabled:bg-gray-300"
          >
            Start OTA Update
          </button>
          
          <button
            onClick={makeFirmwareAvailable}
            disabled={!connected}
            className="px-4 py-2 bg-purple-500 text-white rounded disabled:bg-gray-300"
          >
            Make Firmware Available
          </button>
          
          <button
            onClick={clearLogs}
            className="px-4 py-2 bg-gray-500 text-white rounded"
          >
            Clear Logs
          </button>
        </div>
      </div>

      {/* Action Results */}
      {Object.keys(results).length > 0 && (
        <div className="mb-6 p-4 rounded-lg border">
          <h2 className="text-lg font-semibold mb-4">Action Results</h2>
          <div className="space-y-2">
            {Object.entries(results).map(([key, value]) => (
              <div key={key} className="p-2 bg-gray-50 rounded text-sm">
                <strong>{key}:</strong> {value}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Test Data Info */}
      <div className="mb-6 p-4 rounded-lg border bg-gray-50">
        <h2 className="text-lg font-semibold mb-2">Test Data</h2>
        <div className="text-sm space-y-1">
          <div><strong>Analyzer ID:</strong> {testAnalyzerId}</div>
          <div><strong>User ID:</strong> {testUserId}</div>
          <div><strong>Firmware ID:</strong> {testFirmwareId}</div>
        </div>
      </div>

      {/* Firmware Notifications */}
      {firmwareNotifications.length > 0 && (
        <div className="mb-6 p-4 rounded-lg border">
          <h2 className="text-lg font-semibold mb-4">Firmware Notifications ({firmwareNotifications.length})</h2>
          <div className="space-y-2 max-h-40 overflow-y-auto">
            {firmwareNotifications.map((notification, index) => (
              <div key={index} className="p-2 bg-purple-50 rounded text-sm">
                <div><strong>Version:</strong> {notification.version}</div>
                <div><strong>Description:</strong> {notification.description}</div>
                <div><strong>Stable:</strong> {notification.isStable ? 'Yes' : 'No'}</div>
                <div><strong>Size:</strong> {notification.fileSize} bytes</div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Current OTA Progress */}
      {otaProgress && (
        <div className="mb-6 p-4 rounded-lg border">
          <h2 className="text-lg font-semibold mb-4">Current OTA Progress</h2>
          <div className="p-4 bg-blue-50 rounded">
            <div className="flex justify-between items-center mb-2">
              <span className="text-lg font-medium">Status: {otaProgress.status}</span>
              <span className="text-lg font-medium">{otaProgress.progress}%</span>
            </div>
            <div className="w-full bg-gray-200 rounded-full h-4 mb-2">
              <div 
                className="bg-blue-600 h-4 rounded-full transition-all duration-300" 
                style={{ width: `${otaProgress.progress}%` }}
              ></div>
            </div>
            <div className="text-sm text-gray-600">{otaProgress.message}</div>
            <div className="text-xs text-gray-500 mt-1">
              Update ID: {otaProgress.updateId}
            </div>
          </div>
        </div>
      )}

      {/* OTA History (Collapsible) */}
      {otaHistory.length > 0 && (
        <details className="p-4 rounded-lg border">
          <summary className="text-lg font-semibold cursor-pointer">
            OTA History ({otaHistory.length} updates)
          </summary>
          <div className="mt-4 space-y-1 max-h-40 overflow-y-auto">
            {otaHistory.map((update, index) => (
              <div key={index} className="p-2 bg-gray-50 rounded text-xs">
                <span className="font-medium">{update.status}</span> - {update.progress}% - {update.message}
              </div>
            ))}
          </div>
        </details>
      )}
    </div>
  );
}