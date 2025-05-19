import { useNavigate } from "react-router-dom";
import { AppRoutes } from "../import";

export default function NotFound() {
    const navigate = useNavigate();

    return (
        <div className="flex items-center justify-center h-screen">
            <div className="text-center">
                <h1 className="text-6xl font-bold text-gray-800 mb-4">404</h1>
                <h2 className="text-3xl font-semibold text-gray-600 mb-4">Page Not Found</h2>
                <p className="text-gray-500 mb-8">
                    Sorry, the page you're looking for doesn't exist.
                </p>
                <button 
                    onClick={() => navigate(AppRoutes.home)}
                    className="bg-blue-500 text-white px-6 py-3 rounded-lg hover:bg-blue-600 transition-colors"
                >
                    Go Back Home
                </button>
            </div>
        </div>
    );
}