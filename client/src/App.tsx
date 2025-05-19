import { Routes, Route, Navigate } from "react-router-dom";
import { AppRoutes, AccessLevel } from "./helpers";
import { HomeDashboard, InitiateLoginPage, CompleteLoginPage } from "./pages";
import { useAuth } from "./hooks/useAuth";
import { AuthContext } from './AuthContext';
import { useSidebar } from "./components/ui/sidebar";
import { RequireAuth } from "./components/RequireAuth";
import { Toaster} from 'sonner';
import { WebSocketTest } from "./components/WebSocketTest";

export default function App() {
    const auth = useAuth();
    const { open } = useSidebar();


    const sidebarClass = auth.isAuthenticated
        ? `w-full transition-all duration-300 ${open ? 'lg:pl-80' : ''}`
        : "w-full";

    return (
        <AuthContext.Provider value={auth}>
            <Toaster richColors position="top-right" />
            <div className={sidebarClass}>
                <div className="p-4">
                    <Routes>
                        <Route path={AppRoutes.homeDashboard} element={
                            <RequireAuth accessLevel={AccessLevel.Protected} element={<HomeDashboard />}/>
                        }/>
                        
                        <Route path="/ws-test" element={<WebSocketTest />} />
                        
                        <Route path={AppRoutes.login} element={
                            auth.isAuthenticated?
                                <Navigate to={AppRoutes.homeDashboard} replace /> :
                                <InitiateLoginPage />
                        } />
                        <Route path={AppRoutes.verifyLogin} element={<CompleteLoginPage />} />
                        <Route path="/" element={
                            <Navigate to={auth.isAuthenticated? AppRoutes.homeDashboard : AppRoutes.login} replace />
                        } />
                        <Route path="*" element={<Navigate to={AppRoutes.login} replace />} />
                    </Routes>
                </div>
            </div>
        </AuthContext.Provider>
    )
}