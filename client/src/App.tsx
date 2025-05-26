import { Routes, Route } from "react-router-dom";
import { Toaster } from 'sonner';
import { useEffect } from 'react';
import { toast } from 'sonner';
import {
    AppRoutes, 
    AccessLevel,
    HomeDashboard, 
    InitiateLoginPage, 
    CompleteLoginPage,
    NotFound,
    AdminPage,
    useAuth,
    AuthContext,
    RequireAuth,
    WebSocketTest,
    useSidebar,
    AppSidebar,
    ThemeToggle
} from "./import";
import { useWebSocket } from "./hooks/useWebsocket";
import { Broadcasts } from "./api/websocket-client";
import OtaTestPage from "./pages/OtaTestPage";

export default function App() {
    const auth = useAuth();
    const { open } = useSidebar();
    const { client, connected } = useWebSocket();

    useEffect(() => {
        if (!client || !connected || !auth.isAuthenticated) return;

        let unsubscribe: (() => void) | undefined;

        const setupFirmwareNotifications = async () => {
            await client.send.firmwareNotificationSubscription({ clientType: 'web' });
            
            unsubscribe = client.on(Broadcasts.firmwareAvailable, (firmware: any) => {
                toast.info('New firmware available!', {
                    description: `Version ${firmware.version}: ${firmware.description}`,
                    duration: 8000
                });
            });
        };

        setupFirmwareNotifications();

        return () => {
            if (unsubscribe) {
                unsubscribe();
            }
        };
    }, [client, connected, auth.isAuthenticated]);

    const sidebarClass = auth.isAuthenticated
        ? `w-full transition-all duration-300 ${open ? 'lg:pl-80' : ''}`
        : "w-full";

    return (
        <AuthContext.Provider value={auth}>
            <Toaster richColors position="top-right" />
            <div className="flex min-h-screen w-full bg-bg-cream">
                {auth?.isAuthenticated && (
                    <div className="relative">
                        <AppSidebar />
                    </div>
                )}
                <main className="flex-1 relative">
                    <div className="absolute top-6 right-6 z-50">
                        <ThemeToggle className="scale-150" />
                    </div>
                    <div className={sidebarClass}>
                        <div className="p-4">
                            <Routes>
                                <Route path={AppRoutes.home} element={
                                    <RequireAuth accessLevel={AccessLevel.Protected} element={<HomeDashboard />}/>
                                }/>
                                <Route path={AppRoutes.admin} element={<AdminPage />} />
                                <Route path="/ws-test" element={<WebSocketTest />} />
                                <Route path="/ota-test" element={<OtaTestPage />} />
                                <Route path={AppRoutes.login} element={<InitiateLoginPage />} />
                                <Route path={AppRoutes.verifyLogin} element={<CompleteLoginPage />} />
                                <Route path={AppRoutes.notFound} element={<NotFound />} />
                            </Routes>
                        </div>
                    </div>
                </main>
            </div>
        </AuthContext.Provider>
    )
}