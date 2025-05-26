import { CheckCircle2 } from 'lucide-react';
import { format } from 'date-fns';
import { formatBytes } from '../../../lib/utils';
import type { FirmwareVersionResponse } from '../../../api/Api';

interface FirmwareListItemProps {
  firmware: FirmwareVersionResponse;
}

export function FirmwareListItem({ firmware }: FirmwareListItemProps) {

  return (
    <div className="p-4 border rounded-lg">
      <div className="space-y-3">
        <div className="flex items-center gap-2">
          <h4 className="font-medium">Version {firmware.version}</h4>
          {firmware.isStable && (
            <span className="inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium bg-green-100 text-green-800 rounded-full">
              <CheckCircle2 className="h-3 w-3" />
              Stable
            </span>
          )}
        </div>
        <p className="text-sm text-muted-foreground">{firmware.description}</p>
        <div className="flex gap-4 text-xs text-muted-foreground">
          <span>Size: {formatBytes(firmware.fileSize)}</span>
          <span>Created: {format(new Date(firmware.createdAt), 'd MMM yyyy')}</span>
          {firmware.createdBy && (
            <span>By: {firmware.createdBy}</span>
          )}
        </div>
      </div>
    </div>
  );
}