import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import { AnalyzerAdmin } from '../components/admin/AnalyzerAdmin';
import { FeatureToggleAdmin } from '../components/admin/FeatureToggleAdmin';
import { LoggingAdmin } from '../components/admin/LoggingAdmin';
import { DiagnosticsAdmin } from '../components/admin/DiagnosticsAdmin';
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
                    <TabsTrigger value="diagnostics">Diagnostics</TabsTrigger>
                    <TabsTrigger value="features">Feature Toggles</TabsTrigger>
                    <TabsTrigger value="logging">Logging</TabsTrigger>
                </TabsList>
                
                <TabsContent value="analyzers" className="mt-6">
                    <AnalyzerAdmin />
                </TabsContent>

                <TabsContent value="diagnostics" className="mt-6">
                    <DiagnosticsAdmin />
                </TabsContent>
                
                <TabsContent value="features" className="mt-6">
                    <FeatureToggleAdmin />
                </TabsContent>
                
                <TabsContent value="logging" className="mt-6">
                    <LoggingAdmin />
                </TabsContent>
            </Tabs>
        </div>
    );
}