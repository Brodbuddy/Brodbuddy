import { Routes, Route } from "react-router-dom";
import { AppRoutes, AccessLevel } from "./helpers";
import { Home, InitiateLoginPage, CompleteLoginPage } from "./pages";
import { RequireAuth } from "./components/RequireAuth";
import { useAuth } from "./hooks/useAuth";
import { AuthContext } from './AuthContext';

export default function App() {
    const auth = useAuth();

    return (
        <AuthContext.Provider value={auth}>
            <Routes>
                <Route path={AppRoutes.home} element={<RequireAuth accessLevel={AccessLevel.User} element={<Home />}/>}/>
                <Route path={AppRoutes.login} element={<InitiateLoginPage />} />
                <Route path={AppRoutes.verifyLogin} element={<CompleteLoginPage />} />
            </Routes>
        </AuthContext.Provider>
    )
}