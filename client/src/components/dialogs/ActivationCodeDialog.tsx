import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import ActivationForm from "../analyzer/ActivationForm";

interface ActivationCodeDialogProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    onSuccess?: () => void;
}

function ActivationCodeDialog({open, onOpenChange, onSuccess}: ActivationCodeDialogProps) {
    const handleSuccess = () => {
        onOpenChange(false);
        if (onSuccess) onSuccess();
    };

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent className="sm:max-w-md">
                <DialogHeader>
                    <DialogTitle>Activate Your Sourdough Analyzer</DialogTitle>
                    <DialogDescription>
                        Enter the 12-character activation code that came with your device
                    </DialogDescription>
                </DialogHeader>
                
                <ActivationForm onSuccess={handleSuccess} />
            </DialogContent>
        </Dialog>
    );
}

export default ActivationCodeDialog;