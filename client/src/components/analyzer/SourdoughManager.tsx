import { useState } from "react";
import { useAuthContext } from "../import";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import ActivationCodeDialog from "../dialogs/ActivationCodeDialog";
import AnalyzerList from "./AnalyzerList";

function SourdoughManager() {
    const {user} = useAuthContext();
    const [activationDialogOpen, setActivationDialogOpen] = useState(false);

    const handleActivateClick = () => {
        setActivationDialogOpen(true);
    };

    if (!user) {
        return null;
    }

    return (
        <div className="container mx-auto max-w-4xl py-6">
            <Card className="border-border shadow-md">
                <CardHeader className="pb-3">
                    <CardTitle className="text-2xl text-accent-foreground">Sourdough Analyzer Dashboard</CardTitle>
                    <CardDescription>
                        Manage your sourdough analyzer devices and monitor their status
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <AnalyzerList onActivateClick={handleActivateClick}/>
                    <ActivationCodeDialog
                        open={activationDialogOpen}
                        onOpenChange={setActivationDialogOpen}
                    />
                </CardContent>
            </Card>
        </div>
    );
}

export default SourdoughManager;