import React, { useState, useRef } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle, Button, Input, Checkbox } from '../../ui';
import { Upload, FileText } from 'lucide-react';
import { formatBytes } from '../../../lib/utils';
import { useFirmwareUpload } from '../../../hooks';
import { toast } from 'sonner';

interface FirmwareUploadFormProps {
  onUploadSuccess: () => void;
}

export function FirmwareUploadForm({ onUploadSuccess }: FirmwareUploadFormProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [formData, setFormData] = useState({
    version: '',
    description: '',
    releaseNotes: '',
    isStable: false
  });
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { uploadFirmware, isUploading } = useFirmwareUpload(onUploadSuccess);

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      if (!file.name.endsWith('.bin')) {
        toast.error('Please select a .bin firmware file');
        return;
      }
      setSelectedFile(file);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!selectedFile) {
      toast.error('Please select a firmware file to upload');
      return;
    }

    if (!formData.version || !formData.description) {
      toast.error('Please fill in version and description');
      return;
    }

    try {
      await uploadFirmware(selectedFile, formData);
      
      setSelectedFile(null);
      setFormData({
        version: '',
        description: '',
        releaseNotes: '',
        isStable: false
      });
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    } catch (error) {
      // Error håndteres i hook 
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Upload New Firmware</CardTitle>
        <CardDescription>
          Upload a new firmware version for ESP32 devices
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <label className="text-sm font-medium">Firmware File (.bin)</label>
            <Input
              ref={fileInputRef}
              type="file"
              accept=".bin"
              onChange={handleFileSelect}
              className="w-full border-border-brown focus:ring-accent-foreground"
            />
            {selectedFile && (
              <div className="flex items-center gap-2 text-sm text-muted-foreground mt-2">
                <FileText className="h-4 w-4" />
                <span>{selectedFile.name}</span>
                <span>({formatBytes(selectedFile.size)})</span>
              </div>
            )}
          </div>

          <div className="space-y-2">
            <label htmlFor="version" className="text-sm font-medium">
              Version
            </label>
            <Input
              id="version"
              placeholder="e.g., 1.0.0"
              value={formData.version}
              onChange={(e) => setFormData({ ...formData, version: e.target.value })}
              required
              className="border-border-brown focus:ring-accent-foreground"
            />
          </div>

          <div className="space-y-2">
            <label htmlFor="description" className="text-sm font-medium">
              Description
            </label>
            <Input
              id="description"
              placeholder="Brief description of this firmware version"
              value={formData.description}
              onChange={(e) => setFormData({ ...formData, description: e.target.value })}
              required
              className="border-border-brown focus:ring-accent-foreground"
            />
          </div>

          <div className="space-y-2">
            <label htmlFor="releaseNotes" className="text-sm font-medium">
              Release Notes (optional)
            </label>
            <textarea
              id="releaseNotes"
              className="w-full min-h-[100px] rounded-md border border-border-brown bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-accent-foreground focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
              placeholder="What's new in this version..."
              value={formData.releaseNotes}
              onChange={(e) => setFormData({ ...formData, releaseNotes: e.target.value })}
            />
          </div>

          <div className="flex items-center space-x-2">
            <Checkbox 
              id="isStable"
              checked={formData.isStable}
              onCheckedChange={(checked) => setFormData({ ...formData, isStable: checked as boolean })}
            />
            <label 
              htmlFor="isStable" 
              className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 cursor-pointer"
            >
              Mark as stable release
            </label>
          </div>

          <Button
            type="submit"
            disabled={!selectedFile || isUploading}
            className="w-full bg-accent-foreground text-primary-foreground hover:bg-accent-foreground/90"
          >
            {isUploading ? (
              <>Uploading...</>
            ) : (
              <>
                <Upload className="h-4 w-4 mr-2" />
                Upload Firmware
              </>
            )}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}