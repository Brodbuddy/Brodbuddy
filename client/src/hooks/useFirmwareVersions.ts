import { useState, useEffect } from 'react';
import { api } from './useHttp';
import { toast } from 'sonner';
import type { FirmwareVersionResponse } from '../api/Api';

export function useFirmwareVersions() {
  const [firmwareVersions, setFirmwareVersions] = useState<FirmwareVersionResponse[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const loadFirmwareVersions = async () => {
    setIsLoading(true);
    try {
      const response = await api.ota.getFirmwareVersions();
      setFirmwareVersions(response.data);
    } catch (error) {
      toast.error('Failed to load firmware versions');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadFirmwareVersions();
  }, []);

  return {
    firmwareVersions,
    isLoading,
    refetch: loadFirmwareVersions
  };
}