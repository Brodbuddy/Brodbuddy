import { useState, useEffect } from 'react';
import { Settings, Calendar, Percent } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '../ui/button';
import { Card } from '../ui/card';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '../ui/dialog';
import { Input } from '../ui/input';
import { api } from '../../hooks/useHttp';
import { 
    FeatureToggleResponse, 
    FeatureToggleUpdateRequest,
    FeatureToggleRolloutRequest
} from '../../api/Api';
import { format } from 'date-fns';

export function FeatureToggleAdmin() {
    const [features, setFeatures] = useState<FeatureToggleResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [editDialogOpen, setEditDialogOpen] = useState(false);
    const [selectedFeature, setSelectedFeature] = useState<FeatureToggleResponse | null>(null);
    const [rolloutPercentage, setRolloutPercentage] = useState<number>(0);
    const [updateLoading, setUpdateLoading] = useState(false);

    const fetchFeatures = async () => {
        try {
            setLoading(true);
            const response = await api.feature.getAllFeatures();
            setFeatures(response.data?.features || []);
        } catch (error) {
            toast.error('Failed to fetch features', {
                description: 'Please try again later'
            });
        } finally {
            setLoading(false);
        }
    };

    const handleToggleFeature = async (featureName: string, isEnabled: boolean) => {
        try {
            const updateRequest: FeatureToggleUpdateRequest = { isEnabled: !isEnabled };
            await api.feature.setFeatureEnabled(featureName, updateRequest);
            await fetchFeatures();
        } catch (error: any) {
            toast.error('Failed to toggle feature', {
                description: error.response?.data?.message || 'Please try again later'
            });
        }
    };

    const handleUpdateRollout = async () => {
        if (!selectedFeature) return;
        
        try {
            setUpdateLoading(true);
            const rolloutRequest: FeatureToggleRolloutRequest = { percentage: rolloutPercentage };
            await api.feature.setRolloutPercentage(selectedFeature.name, rolloutRequest);
            await fetchFeatures();
            setEditDialogOpen(false);
            setSelectedFeature(null);
        } catch (error: any) {
            toast.error('Failed to update rollout', {
                description: error.response?.data?.message || 'Please try again later'
            });
        } finally {
            setUpdateLoading(false);
        }
    };

    const openEditDialog = (feature: FeatureToggleResponse) => {
        setSelectedFeature(feature);
        setRolloutPercentage(feature.rolloutPercentage || 0);
        setEditDialogOpen(true);
    };

    useEffect(() => {
        fetchFeatures();
    }, []);

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-2xl font-bold">Feature Toggles</h2>
                    <p className="text-muted-foreground">Control available endpoints</p>
                </div>
            </div>

            {loading ? (
                <div className="text-center py-8">Loading features...</div>
            ) : (
                <div className="grid gap-4">
                    {features.length === 0 ? (
                        <Card className="p-8 text-center border-border shadow-md">
                            <p className="text-muted-foreground">No features found</p>
                        </Card>
                    ) : (
                        features.map((feature) => (
                            <Card key={feature.id} className="p-4 border-border shadow-md">
                                <div className="flex items-center justify-between">
                                    <div className="flex-1">
                                        <div className="flex items-center gap-2">
                                            <h3 className="font-semibold">{feature.name}</h3>
                                            <span className={`text-xs px-2 py-1 rounded-full ${
                                                feature.isEnabled 
                                                    ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300' 
                                                    : 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300'
                                            }`}>
                                                {feature.isEnabled ? 'Enabled' : 'Disabled'}
                                            </span>
                                        </div>
                                        {feature.description && (
                                            <p className="text-sm text-muted-foreground mt-1">{feature.description}</p>
                                        )}
                                        <div className="flex items-center gap-4 mt-2 text-sm text-muted-foreground">
                                            <div className="flex items-center gap-1">
                                                <Calendar className="w-4 h-4" />
                                                <span>Created: {format(new Date(feature.createdAt), 'MMM d, yyyy')}</span>
                                            </div>
                                            {feature.rolloutPercentage !== null && (
                                                <div className="flex items-center gap-1">
                                                    <Percent className="w-4 h-4" />
                                                    <span>Rollout: {feature.rolloutPercentage}%</span>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                    <div className="flex items-center gap-2">
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            className="dark:text-white dark:hover:bg-gray-600 dark:hover:text-white [&]:dark:text-white [&]:dark:hover:text-white"
                                            onClick={() => openEditDialog(feature)}
                                        >
                                            <Settings className="w-4 h-4 mr-2" />
                                            Configure
                                        </Button>
                                        <Button
                                            size="sm"
                                            variant={feature.isEnabled ? "destructive" : "default"}
                                            className={feature.isEnabled ? '' : 'bg-green-500 text-white hover:bg-green-600 dark:bg-green-600 dark:text-white dark:hover:bg-green-700'}
                                            onClick={() => handleToggleFeature(feature.name, feature.isEnabled)}
                                        >
                                            {feature.isEnabled ? 'Disable' : 'Enable'}
                                        </Button>
                                    </div>
                                </div>
                            </Card>
                        ))
                    )}
                </div>
            )}

            <Dialog open={editDialogOpen} onOpenChange={setEditDialogOpen}>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>Configure Feature: {selectedFeature?.name}</DialogTitle>
                    </DialogHeader>
                    {selectedFeature && (
                        <div className="space-y-4">
                            <div>
                                <label className="text-sm font-medium">Description</label>
                                <p className="text-sm text-muted-foreground mt-1">
                                    {selectedFeature.description || 'No description available'}
                                </p>
                            </div>
                            <div>
                                <label className="text-sm font-medium mb-2 block">Rollout Percentage</label>
                                <div className="flex items-center gap-2">
                                    <Input
                                        type="number"
                                        min="0"
                                        max="100"
                                        value={rolloutPercentage}
                                        onChange={(e) => setRolloutPercentage(Number(e.target.value))}
                                        className="flex-1"
                                    />
                                    <span className="text-sm text-muted-foreground">%</span>
                                </div>
                                <p className="text-xs text-muted-foreground mt-1">
                                    Percentage of users who will see this feature
                                </p>
                            </div>
                            <Button 
                                onClick={handleUpdateRollout}
                                disabled={updateLoading}
                                className="w-full bg-orange-500 text-black hover:bg-orange-600 dark:bg-orange-600 dark:text-white"
                            >
                                {updateLoading ? 'Updating...' : 'Update Rollout'}
                            </Button>
                        </div>
                    )}
                </DialogContent>
            </Dialog>
        </div>
    );
}