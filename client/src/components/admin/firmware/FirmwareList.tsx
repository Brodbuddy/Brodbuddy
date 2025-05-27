import { Card, CardContent, CardDescription, CardHeader, CardTitle, Button } from '../../ui';
import { FirmwareListItem } from './FirmwareListItem';
import { Send } from 'lucide-react';
import { useWebSocket } from '../../../hooks/useWebsocket';
import { toast } from 'sonner';
import type { FirmwareVersionResponse } from '../../../api/Api';

interface FirmwareListProps {
  firmwareVersions: FirmwareVersionResponse[];
  isLoading: boolean;
}

export function FirmwareList({ firmwareVersions, isLoading }: FirmwareListProps) {
  const { client } = useWebSocket();

  const handleReleaseLatest = async () => {
    if (!client) {
      toast.error('WebSocket not connected');
      return;
    }

    const latestStable = firmwareVersions
      .filter(f => f.isStable)
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())[0];

    if (!latestStable) {
      toast.error('No stable firmware available to release');
      return;
    }

    try {
      await client.send.makeFirmwareAvailable({ firmwareId: latestStable.id });
      toast.success(`Released latest firmware ${latestStable.version} to all users`);
    } catch (error) {
      toast.error('Failed to release firmware');
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle>Firmware Versions</CardTitle>
            <CardDescription>
              Manage uploaded firmware versions
            </CardDescription>
          </div>
          {firmwareVersions.some(f => f.isStable) && (
            <Button
              onClick={handleReleaseLatest}
              className="bg-accent-foreground text-primary-foreground hover:bg-accent-foreground/90"
            >
              <Send className="h-4 w-4 mr-2" />
              Release Latest
            </Button>
          )}
        </div>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <div className="text-center py-4">Loading...</div>
        ) : firmwareVersions.length === 0 ? (
          <div className="text-center py-8 text-muted-foreground">
            No firmware versions uploaded yet
          </div>
        ) : (
          <div className="space-y-4">
            {firmwareVersions.map((firmware) => (
              <FirmwareListItem
                key={firmware.id}
                firmware={firmware}
              />
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}