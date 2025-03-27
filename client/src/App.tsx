import { Routes, Route } from "react-router-dom";
import { AppRoutes } from "./helpers";
import { Home } from "./pages";

export default function App() {
    return (
        <Routes>
            <Route path={AppRoutes.Home} element={<Home />} />
        </Routes>
    )
}