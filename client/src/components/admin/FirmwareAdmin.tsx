import { FirmwareUploadForm, FirmwareList } from './firmware';
import { useFirmwareVersions } from '../../hooks';

export function FirmwareAdmin() {
  const { firmwareVersions, isLoading, refetch } = useFirmwareVersions();

  return (
    <div className="space-y-6">
      <FirmwareUploadForm onUploadSuccess={refetch} />
      <FirmwareList
        firmwareVersions={firmwareVersions}
        isLoading={isLoading}
      />
    </div>
  );
}