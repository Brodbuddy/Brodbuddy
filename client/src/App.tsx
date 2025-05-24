import { Routes, Route } from "react-router-dom";
import { Toaster } from 'sonner';
import {
    Home, 
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

export default function App() {
    const auth = useAuth();
    const { open } = useSidebar();

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
                                <Route path={AppRoutes.home} element={<Home />} />
                                <Route path={AppRoutes.homeDashboard} element={
                                    <RequireAuth accessLevel={AccessLevel.Protected} element={<HomeDashboard />}/>
                                }/>
                                <Route path={AppRoutes.admin} element={<AdminPage />} />
                                <Route path="/ws-test" element={<WebSocketTest />} />
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