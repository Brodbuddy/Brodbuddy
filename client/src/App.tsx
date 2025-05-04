import { Routes, Route } from "react-router-dom";
import { AppRoutes } from "./helpers";
import { Home, InitiateLoginPage, CompleteLoginPage } from "./pages";
import { RequireAuth } from "./components/RequireAuth";
import { AccessLevel } from "./atoms/auth";

export default function App() {
    return (
        <Routes>
            <Route path={AppRoutes.home} element={<RequireAuth accessLevel={AccessLevel.User} element={<Home />}/>}/>
            <Route path={AppRoutes.login} element={<InitiateLoginPage />} />
            <Route path={AppRoutes.verifyLogin} element={<CompleteLoginPage />} />
        </Routes>
    )
}