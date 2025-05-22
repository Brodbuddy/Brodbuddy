import { Moon, Sun } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";


const getInitialTheme = (): "light" | "dark" => {
    if (typeof window === "undefined") return "light";
    
    const storedTheme = localStorage.getItem("theme") as "light" | "dark" | null;
    if (storedTheme) {
        return storedTheme;
    }
    
    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
};

export function ThemeToggle({ className }: { className?: string }) {
    const [theme, setTheme] = useState<"light" | "dark">(getInitialTheme);

    useEffect(() => {
        const initialTheme = getInitialTheme();
        setTheme(initialTheme);
        
        const root = window.document.documentElement;
        root.classList.remove("light", "dark");
        root.classList.add(initialTheme);
    }, []);

    useEffect(() => {
        const root = window.document.documentElement;
        root.classList.remove("light", "dark");
        root.classList.add(theme);
        localStorage.setItem("theme", theme);
    }, [theme]);

    return (
        <Button
            variant="ghost"
            size="icon"
            onClick={() => setTheme(theme === "light" ? "dark" : "light")}
            aria-label={`Switch to ${theme === "light" ? "dark" : "light"} theme`}

            className={cn("p-3", className)}
        >
            {theme === "light" ? (
                <Moon className="h-7 w-7" />
                ) : (
                <Sun className="h-7 w-7" />
                )}
        </Button>
    );
}