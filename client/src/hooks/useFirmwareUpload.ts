import { useState } from 'react';
import { api } from './useHttp';
import { toast } from 'sonner';

interface FirmwareUploadData {
  version: string;
  description: string;
  releaseNotes: string;
  isStable: boolean;
}

export function useFirmwareUpload(onSuccess?: () => void) {
  const [isUploading, setIsUploading] = useState(false);

  const uploadFirmware = async (file: File, formData: FirmwareUploadData) => {
    setIsUploading(true);
    try {
      await api.ota.uploadFirmware({
        File: file,
        Version: formData.version,
        Description: formData.description,
        ReleaseNotes: formData.releaseNotes || null,
        IsStable: formData.isStable
      });
      
      toast.success('Firmware uploaded successfully');

      onSuccess?.();
    } catch (error) {
      toast.error('Failed to upload firmware');
      throw error;
    } finally {
      setIsUploading(false);
    }
  };

  return {
    uploadFirmware,
    isUploading
  };
}