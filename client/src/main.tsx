import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from "react-router-dom"
import App from './App.tsx'
import './index.css'
import { SidebarProvider } from "./components/ui/sidebar"
import { AppSidebar } from "./components/Sidebar/app-sidebar"
import { useAuth } from "./hooks/useAuth"


export function Layout() {
    const auth = useAuth();

    return (
        <div className="flex min-h-screen w-full bg-bg-cream">

            {auth?.isAuthenticated && (
                <div className="relative">
                    <AppSidebar />
                </div>
            )}
            <main className="flex-1">
                <App />
            </main>
        </div>
    );
}

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <BrowserRouter>
            <SidebarProvider>
                <Layout />
            </SidebarProvider>
        </BrowserRouter>
    </StrictMode>
)