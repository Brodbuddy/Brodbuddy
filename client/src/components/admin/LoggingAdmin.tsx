import { useState, useEffect } from 'react';
import { Activity, Save } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '../ui/button';
import { Card } from '../ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { api } from '../../hooks/useHttp';
import { LoggingLevel, LogLevelUpdateRequest } from '../../api/Api';

const LOG_LEVEL_DESCRIPTIONS = {
    [LoggingLevel.Verbose]: 'Most detailed logging - includes all information',
    [LoggingLevel.Debug]: 'Detailed information for debugging purposes',
    [LoggingLevel.Information]: 'General information about application flow',
    [LoggingLevel.Warning]: 'Potentially harmful situations',
    [LoggingLevel.Error]: 'Error events but application continues',
    [LoggingLevel.Fatal]: 'Very severe error events that may abort the application'
};

const LOG_LEVEL_COLORS = {
    [LoggingLevel.Verbose]: 'bg-gray-100 text-gray-800 dark:bg-black dark:text-gray-400',
    [LoggingLevel.Debug]: 'bg-blue-100 text-blue-800 dark:bg-black dark:text-blue-400',
    [LoggingLevel.Information]: 'bg-green-100 text-green-800 dark:bg-black dark:text-green-400',
    [LoggingLevel.Warning]: 'bg-yellow-100 text-yellow-800 dark:bg-black dark:text-yellow-400',
    [LoggingLevel.Error]: 'bg-red-100 text-red-800 dark:bg-black dark:text-red-400',
    [LoggingLevel.Fatal]: 'bg-red-200 text-red-900 dark:bg-black dark:text-red-500'
};

export function LoggingAdmin() {
    const [currentLevel, setCurrentLevel] = useState<LoggingLevel | null>(null);
    const [selectedLevel, setSelectedLevel] = useState<LoggingLevel | null>(null);
    const [loading, setLoading] = useState(true);
    const [updateLoading, setUpdateLoading] = useState(false);

    const fetchCurrentLogLevel = async () => {
        try {
            setLoading(true);
            const response = await api.logging.getCurrentLogLevel();
            const level = response.data?.currentLevel;
            setCurrentLevel(level);
            setSelectedLevel(level);
        } catch (error) {
            toast.error('Failed to fetch log level', {
                description: 'Please try again later'
            });
        } finally {
            setLoading(false);
        }
    };

    const handleUpdateLogLevel = async () => {
        if (!selectedLevel) return;
        
        try {
            setUpdateLoading(true);
            const updateRequest: LogLevelUpdateRequest = { logLevel: selectedLevel };
            const response = await api.logging.setLogLevel(updateRequest);
            if (response.data) {
                setCurrentLevel(selectedLevel);
            }
        } catch (error: any) {
            toast.error('Failed to update log level', {
                description: error.response?.data?.message || 'Please try again later'
            });
        } finally {
            setUpdateLoading(false);
        }
    };

    useEffect(() => {
        fetchCurrentLogLevel();
    }, []);

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-2xl font-bold">Logging Configuration</h2>
                    <p className="text-muted-foreground">Control application logging verbosity</p>
                </div>
            </div>

            {loading ? (
                <div className="text-center py-8">Loading logging configuration...</div>
            ) : (
                <div className="grid gap-6">
                    <Card className="p-6 border-border shadow-md">
                        <div className="space-y-4">
                            <div className="flex items-center gap-2">
                                <Activity className="w-5 h-5" />
                                <h3 className="text-lg font-semibold">Current Log Level</h3>
                            </div>
                            
                            {currentLevel && (
                                <div className="flex items-center gap-3">
                                    <span className={`text-sm px-3 py-1 rounded-full font-medium ${LOG_LEVEL_COLORS[currentLevel]}`}>
                                        {currentLevel}
                                    </span>
                                    <span className="text-sm text-muted-foreground">
                                        {LOG_LEVEL_DESCRIPTIONS[currentLevel]}
                                    </span>
                                </div>
                            )}
                        </div>
                    </Card>

                    <Card className="p-6 border-border shadow-md">
                        <div className="space-y-4">
                            <h3 className="text-lg font-semibold">Update Log Level</h3>
                            
                            <div className="space-y-3">
                                <div>
                                    <label className="text-sm font-medium mb-2 block">Select New Log Level</label>
                                    <Select 
                                        value={selectedLevel || ''} 
                                        onValueChange={(value) => setSelectedLevel(value as LoggingLevel)}
                                    >
                                        <SelectTrigger className="w-full text-primary">
                                            <SelectValue placeholder="Choose log level" />
                                        </SelectTrigger>
                                        <SelectContent className="bg-white dark:bg-white text-black">
                                            {Object.values(LoggingLevel).map((level) => (
                                                <SelectItem key={level} value={level}>
                                                    <div className="flex items-center gap-2">
                                                        <span className={`text-xs px-2 py-1 rounded ${LOG_LEVEL_COLORS[level]}`}>
                                                            {level}
                                                        </span>
                                                        <span className="text-sm text-gray-700 dark:text-gray-300">
                                                            {LOG_LEVEL_DESCRIPTIONS[level]}
                                                        </span>
                                                    </div>
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>

                                {selectedLevel && selectedLevel !== currentLevel && (
                                    <div className="p-3 bg-blue-50 border border-blue-200 rounded-md">
                                        <div className="flex items-center gap-2">
                                            <span className="text-sm font-medium text-blue-800">Preview:</span>
                                            <span className={`text-xs px-2 py-1 rounded ${LOG_LEVEL_COLORS[selectedLevel]}`}>
                                                {selectedLevel}
                                            </span>
                                        </div>
                                        <p className="text-sm text-blue-700 mt-1">
                                            {LOG_LEVEL_DESCRIPTIONS[selectedLevel]}
                                        </p>
                                    </div>
                                )}

                                <Button
                                    onClick={handleUpdateLogLevel}
                                    disabled={updateLoading || !selectedLevel || selectedLevel === currentLevel}
                                    className="w-full bg-accent-foreground text-primary-foreground hover:bg-accent-foreground/90"
                                >
                                    <Save className="w-4 h-4 mr-2" />
                                    {updateLoading ? 'Updating...' : 'Update Log Level'}
                                </Button>
                            </div>
                        </div>
                    </Card>

                    <Card className="p-6 border-border shadow-md">
                        <div className="space-y-3">
                            <h3 className="text-lg font-semibold">Log Level Reference</h3>
                            <div className="grid gap-2">
                                {Object.values(LoggingLevel).map((level) => (
                                    <div key={level} className="flex items-center gap-3 p-2 rounded border border-border">
                                        <span className={`text-xs px-2 py-1 rounded font-medium ${LOG_LEVEL_COLORS[level]}`}>
                                            {level}
                                        </span>
                                        <span className="text-sm text-muted-foreground flex-1">
                                            {LOG_LEVEL_DESCRIPTIONS[level]}
                                        </span>
                                    </div>
                                ))}
                            </div>
                        </div>
                    </Card>
                </div>
            )}
        </div>
    );
}