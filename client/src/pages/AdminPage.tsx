import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import { AnalyzerAdmin, FeatureToggleAdmin, LoggingAdmin, FirmwareAdmin } from '../components/admin';
import { useAtom } from 'jotai';
import { adminTabAtom, AdminTab } from '../atoms/adminTab';

export function AdminPage() {
    const [activeTab, setActiveTab] = useAtom(adminTabAtom);

    const handleTabChange = (value: string) => {
        setActiveTab(value as AdminTab);
    };

    return (
        <div className="p-6 max-w-7xl mx-auto">
            <div className="mb-6">
                <h1 className="text-3xl font-bold">Admin Dashboard</h1>
                <p className="text-muted-foreground">Manage system configuration and monitoring</p>
            </div>
            
            <Tabs value={activeTab} onValueChange={handleTabChange} className="w-full">
                <TabsList className="grid w-full grid-cols-4">
                    <TabsTrigger value="analyzers">Sourdough Analyzers</TabsTrigger>
                    <TabsTrigger value="features">Feature Toggles</TabsTrigger>
                    <TabsTrigger value="logging">Logging</TabsTrigger>
                    <TabsTrigger value="firmware">Firmware</TabsTrigger>
                </TabsList>
                
                <TabsContent value="analyzers" className="mt-6">
                    <AnalyzerAdmin />
                </TabsContent>
                
                <TabsContent value="features" className="mt-6">
                    <FeatureToggleAdmin />
                </TabsContent>
                
                <TabsContent value="logging" className="mt-6">
                    <LoggingAdmin />
                </TabsContent>
                
                <TabsContent value="firmware" className="mt-6">
                    <FirmwareAdmin />
                </TabsContent>
            </Tabs>
        </div>
    );
}