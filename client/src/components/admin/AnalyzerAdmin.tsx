import { useState, useEffect } from 'react';
import { Plus, Loader2, Check, Copy } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { toast } from 'sonner';
import { Button } from '../ui/button';
import { Card } from '../ui/card';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogDescription } from '../ui/dialog';
import { Input } from '../ui/input';
import { api } from '../../hooks/useHttp';
import { AdminAnalyzerListResponse } from '../../api/Api';
import { format } from 'date-fns';

const createAnalyzerSchema = yup.object({
    macAddress: yup
        .string()
        .required('MAC address is required')
        .matches(/^([0-9A-F]{2}:){5}[0-9A-F]{2}$/, 'MAC address must be in format XX:XX:XX:XX:XX:XX'),
    name: yup
        .string()
        .required('Name is required')
        .min(2, 'Name must be at least 2 characters')
        .max(50, 'Name must be less than 50 characters')
});

type CreateAnalyzerFormValues = {
    macAddress: string;
    name: string;
};

export function AnalyzerAdmin() {
    const [analyzers, setAnalyzers] = useState<AdminAnalyzerListResponse[]>([]);
    const [loading, setLoading] = useState(true);
    const [createDialogOpen, setCreateDialogOpen] = useState(false);
    const [createSuccess, setCreateSuccess] = useState(false);

    const form = useForm<CreateAnalyzerFormValues>({
        resolver: yupResolver(createAnalyzerSchema) as any,
        defaultValues: {
            macAddress: '',
            name: ''
        }
    });

    const formatMacAddress = (value: string) => {
        const cleaned = value.replace(/[^A-F0-9]/gi, '').toUpperCase();
        const chunks = cleaned.match(/.{1,2}/g) || [];
        return chunks.slice(0, 6).join(':');
    };

    const copyToClipboard = async (text: string, label: string) => {
        try {
            await navigator.clipboard.writeText(text);
            toast.success(`${label} copied to clipboard!`);
        } catch {
            toast.error('Failed to copy to clipboard');
        }
    };

    const fetchAnalyzers = async () => {
        try {
            setLoading(true);
            const response = await api.analyzer.getAllAnalyzers();
            setAnalyzers(response.data || []);
        } catch (error) {
            console.error('Failed to fetch analyzers:', error);
            toast.error('Failed to fetch analyzers', {
                description: 'Please try again later'
            });
        } finally {
            setLoading(false);
        }
    };

    const onSubmit = async (data: CreateAnalyzerFormValues) => {
        try {
            const response = await api.analyzer.createAnalyzer(data);
            
            if (response.data) {
                setCreateSuccess(true);
                toast.success('Analyzer created successfully!', {
                    description: `${data.name} with activation code ${response.data.activationCode}`
                });
                
                setTimeout(() => {
                    form.reset();
                    setCreateSuccess(false);
                    setCreateDialogOpen(false);
                    fetchAnalyzers();
                }, 2000);
            }
        } catch (error: any) {
            toast.error('Failed to create analyzer', {
                description: error.response?.data?.message || 'Please try again later'
            });
        }
    };

    useEffect(() => {
        fetchAnalyzers();
    }, []);

    return (
        <div className="space-y-6">
            <div className="flex justify-between items-center">
                <div>
                    <h2 className="text-2xl font-bold">Sourdough Analyzers</h2>
                    <p className="text-muted-foreground">Manage analyzer devices and registrations</p>
                </div>
                <Dialog open={createDialogOpen} onOpenChange={setCreateDialogOpen}>
                    <DialogTrigger asChild>
                        <Button className="bg-orange-500 text-black hover:bg-orange-600 dark:bg-orange-600 dark:text-white dark:hover:bg-orange-700 dark:hover:text-white">
                            <Plus className="w-4 h-4 mr-2 dark:text-white" />
                            Add Analyzer
                        </Button>
                    </DialogTrigger>
                    <DialogContent className="sm:max-w-md">
                        <DialogHeader>
                            <DialogTitle>Create New Analyzer</DialogTitle>
                            <DialogDescription>
                                Add a new sourdough analyzer device to the system
                            </DialogDescription>
                        </DialogHeader>
                        
                        <form onSubmit={form.handleSubmit(onSubmit)}>
                            <div className="space-y-4">
                                <div className="space-y-2">
                                    <label htmlFor="macAddress" className="text-sm font-medium">
                                        MAC Address
                                    </label>
                                    <Input
                                        id="macAddress"
                                        placeholder="XX:XX:XX:XX:XX:XX"
                                        className="font-mono tracking-wider border border-gray-300 dark:border-gray-800"
                                        disabled={form.formState.isSubmitting || createSuccess}
                                        {...form.register('macAddress')}
                                        onChange={(e) => {
                                            const formatted = formatMacAddress(e.target.value);
                                            if (formatted.length <= 17) {
                                                form.setValue('macAddress', formatted);
                                            }
                                        }}
                                    />
                                    {form.formState.errors.macAddress && (
                                        <p className="text-red-500 text-sm">{form.formState.errors.macAddress.message}</p>
                                    )}
                                    <p className="text-sm text-muted-foreground">
                                        Enter the device's MAC address
                                    </p>
                                </div>

                                <div className="space-y-2">
                                    <label htmlFor="name" className="text-sm font-medium">
                                        Analyzer Name
                                    </label>
                                    <Input
                                        id="name"
                                        placeholder="Kitchen Analyzer"
                                        className="border border-gray-300 dark:border-gray-800"
                                        disabled={form.formState.isSubmitting || createSuccess}
                                        {...form.register('name')}
                                        maxLength={50}
                                    />
                                    {form.formState.errors.name && (
                                        <p className="text-red-500 text-sm">{form.formState.errors.name.message}</p>
                                    )}
                                    <p className="text-sm text-muted-foreground">
                                        Choose a name for this analyzer device
                                    </p>
                                </div>

                                <Button
                                    type="submit"
                                    disabled={form.formState.isSubmitting || createSuccess}
                                    className="w-full bg-orange-500 text-black hover:bg-orange-600 dark:bg-orange-600 dark:text-white mt-4"
                                >
                                    {form.formState.isSubmitting ? (
                                        <>
                                            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                            Creating...
                                        </>
                                    ) : createSuccess ? (
                                        <>
                                            <Check className="mr-2 h-4 w-4" />
                                            Created!
                                        </>
                                    ) : (
                                        'Create Analyzer'
                                    )}
                                </Button>
                            </div>
                        </form>
                    </DialogContent>
                </Dialog>
            </div>

            {loading ? (
                <div className="text-center py-8">Loading analyzers...</div>
            ) : (
                <div className="grid gap-4">
                    {analyzers.length === 0 ? (
                        <Card className="p-8 text-center">
                            <p className="text-muted-foreground">No analyzers found</p>
                        </Card>
                    ) : (
                        analyzers.map((analyzer) => (
                            <Card key={analyzer.id} className="p-6 border-border shadow-md">
                                <div className="space-y-4">
                                    <div className="flex items-start justify-between">
                                        <div className="flex-1">
                                            <div className="flex items-center gap-3 mb-2 flex-wrap">
                                                <h3 className="text-lg font-semibold">{analyzer.name}</h3>
                                                <div className="flex items-center gap-2">
                                                    <span className={`text-xs px-2 py-1 rounded-full font-medium ${
                                                        analyzer.isActivated
                                                            ? 'bg-green-100 text-green-800 dark:bg-slate-800 dark:text-green-400'
                                                            : 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-400'
                                                    }`}>
                                                        {analyzer.isActivated ? 'Activated' : 'Not Activated'}
                                                    </span>
                                                    {analyzer.activationCode && !analyzer.isActivated && (
                                                        <div className="flex items-center gap-1 bg-blue-50 dark:bg-blue-900/20 px-2 py-1 rounded border">
                                                            <span className="text-xs font-mono text-blue-700 dark:text-blue-300">
                                                                {analyzer.activationCode}
                                                            </span>
                                                            <Button
                                                                variant="ghost"
                                                                size="sm"
                                                                className="h-4 w-4 p-0 hover:bg-blue-100 dark:hover:bg-blue-800/30"
                                                                onClick={() => copyToClipboard(analyzer.activationCode!, 'Activation code')}
                                                            >
                                                                <Copy className="h-3 w-3 text-blue-600 dark:text-blue-400" />
                                                            </Button>
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                    
                                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
                                        <div>
                                            <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">ID</label>
                                            <p className="font-mono text-xs mt-1">{analyzer.id}</p>
                                        </div>
                                        
                                        <div>
                                            <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">MAC Address</label>
                                            <p className="font-mono mt-1">{analyzer.macAddress}</p>
                                        </div>
                                        
                                        <div>
                                            <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Firmware Version</label>
                                            <p className="mt-1">{analyzer.firmwareVersion || 'Unknown'}</p>
                                        </div>
                                        
                                        <div>
                                            <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Created</label>
                                            <p className="mt-1">{format(new Date(analyzer.createdAt), 'dd/MM/yyyy')}</p>
                                        </div>
                                        
                                        {analyzer.activatedAt && (
                                            <div>
                                                <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Activated</label>
                                                <p className="mt-1">{format(new Date(analyzer.activatedAt), 'dd/MM/yyyy')}</p>
                                            </div>
                                        )}
                                        
                                        {analyzer.lastSeen && (
                                            <div>
                                                <label className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Last Seen</label>
                                                <p className="mt-1">{format(new Date(analyzer.lastSeen), 'dd/MM/yyyy')}</p>
                                            </div>
                                        )}
                                    </div>
                                </div>
                            </Card>
                        ))
                    )}
                </div>
            )}
        </div>
    );
}