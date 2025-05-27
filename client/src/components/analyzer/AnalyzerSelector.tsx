import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useAtomValue } from 'jotai';
import { analyzersAtom } from '@/atoms';

interface AnalyzerSelectorProps {
    selectedAnalyzerId?: string;
    onAnalyzerChange: (analyzerId: string) => void;
    className?: string;
}

export function AnalyzerSelector({
                                     selectedAnalyzerId,
                                     onAnalyzerChange,
                                     className
                                 }: AnalyzerSelectorProps) {
    const analyzers = useAtomValue(analyzersAtom);

    if (!analyzers || analyzers.length === 0) {
        return (
            <div className="text-sm text-muted-foreground italic">
                No analyzers available
            </div>
        );
    }

    return (
        <Select value={selectedAnalyzerId || ''} onValueChange={onAnalyzerChange}>
            <SelectTrigger className={className}>
                <SelectValue placeholder="Select analyzer" />
            </SelectTrigger>
            <SelectContent>
                {analyzers.map((analyzer) => (
                    <SelectItem key={analyzer.id} value={analyzer.id}>
                        <div className="flex items-center gap-2">
                            <span>{analyzer.nickname || analyzer.name}</span>
                            {analyzer.isOwner && (
                                <span className="text-xs bg-orange-100 text-orange-800 px-1.5 py-0.5 rounded-full">
                                    Owner
                                </span>
                            )}
                        </div>
                    </SelectItem>
                ))}
            </SelectContent>
        </Select>
    );
}