import { useState, useEffect } from "react";
import { format } from "date-fns";
import { useAtom } from "jotai";
import { toast } from "sonner";
import { PlusCircle } from "lucide-react";
import { api, analyzersAtom } from "../import";
import { Button } from "@/components/ui/button";
import ActivationForm from "./ActivationForm";

interface AnalyzerListProps {
    onActivateClick: () => void;
}

function AnalyzerList({onActivateClick}: AnalyzerListProps) {
    const [analyzers, setAnalyzers] = useAtom(analyzersAtom);
    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        loadAnalyzers();
    }, []);

    const loadAnalyzers = async () => {
        setIsLoading(true);
        try {
            const response = await api.analyzer.getUserAnalyzers();
            setAnalyzers(response.data);
        } catch (error: any) {
            toast.error("Failed to load analyzers", {
                description: error.response?.data?.message || "Please try again later"
            });
        } finally {
            setIsLoading(false);
        }
    };

    if (isLoading) {
        return (
            <div className="space-y-4">
                <div className="flex items-center justify-between">
                    <h3 className="text-lg font-medium">Your Devices</h3>
                </div>
                <div className="space-y-3">
                    {[1, 2].map((n) => (
                        <div key={n} className="h-20 rounded-lg border p-4 animate-pulse bg-gray-100 dark:bg-gray-800"/>
                    ))}
                </div>
            </div>
        );
    }

    if (!analyzers?.length) {
        return (
            <div className="mt-4 rounded-lg border p-6">
                <div className="mb-6">
                    <h2 className="text-xl font-semibold mb-1">Activate Your Device</h2>
                    <p className="text-muted-foreground">
                        Enter your 12-character activation code and choose a nickname for your device.
                    </p>
                </div>
                
                <div className="max-w-md mx-auto">
                    <ActivationForm onSuccess={() => {}} />
                </div>
                
                <div className="mt-4 pt-4 border-t text-sm text-muted-foreground">
                    Having trouble? Check the activation code on the bottom of your device or contact support.
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-4 mt-8">
            <div className="flex items-center justify-between">
                <h3 className="text-lg font-medium">Your Devices</h3>
                <Button onClick={onActivateClick} className="bg-orange-500 text-black hover:bg-orange-600 dark:bg-orange-600 dark:text-white text-sm px-3 py-1 h-8">
                    <PlusCircle className="mr-2 h-4 w-4"/>
                    Add Device
                </Button>
            </div>
            <div className="space-y-3">
                {analyzers.map((analyzer) => (
                    <div key={analyzer.id} className="flex items-center justify-between rounded-lg border p-4 hover:border-accent/50 transition-colors">
                        <div>
                            <p className="font-medium">
                                {analyzer.nickname || analyzer.name}{" "}
                                {analyzer.isOwner && (
                                    <span className="text-xs bg-accent/20 text-accent-foreground px-2 py-0.5 rounded-full ml-2">Owner</span>
                                )}
                            </p>
                            {analyzer.lastSeen && (
                                <p className="text-sm text-muted-foreground">
                                    Last active: {format(new Date(analyzer.lastSeen), "MMM d, yyyy")}
                                </p>
                            )}
                        </div>
                        <Button variant="outline" size="sm" className="border-border hover:bg-accent hover:text-accent-foreground">
                            View
                        </Button>
                    </div>
                ))}
            </div>
        </div>
    );
}

export default AnalyzerList;